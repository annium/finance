using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Annium.Data.Tables;
using Annium.Finance.Providers.Abstractions.Connectors.Shared;
using Annium.Finance.Providers.Abstractions.Connectors.User;
using Annium.Finance.Providers.Abstractions.Domain.Shared.Operations;
using Annium.Finance.Providers.Abstractions.Domain.User;
using Annium.Finance.Providers.Abstractions.Domain.User.Operations;
using Annium.Finance.Providers.Abstractions.Domain.User.Requests;
using Annium.Finance.Providers.Core.Shared.Loaders;
using Annium.Finance.Providers.Core.Shared.RateLimits;
using Annium.Finance.Providers.Core.Shared.Status;
using Annium.Finance.Providers.Core.User;
using Annium.Finance.Providers.Crypto.Binance.Base.Shared.Contracts.Domain;
using Annium.Finance.Providers.Crypto.Binance.Base.Shared.HttpExtensions;
using Annium.Finance.Providers.Crypto.Binance.Base.User;
using Annium.Finance.Providers.Crypto.Binance.Base.User.Contracts.Domain;
using Annium.Finance.Providers.Crypto.Binance.Base.User.Services;
using Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Connectors.Extensions;
using Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Contracts.User.Domain;
using Annium.Logging;
using Annium.Net.Http;
using Annium.Serialization.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using static System.Net.Mime.MediaTypeNames;
using static Annium.Finance.Providers.Crypto.Binance.UsdFutures.Constants;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Connectors;

internal class UserConnector : UserConnectorBase, IUserConnector
{
    private readonly UserConfig _config;
    private readonly QueryProcessor _queryProcessor;
    private readonly SignatureService _signatureService;
    private readonly IHttpRequestFactory _setLeverageRequestFactory;
    private readonly IHttpRequestFactory _initOrderRequestFactory;
    private readonly IHttpRequestFactory _modifyOrderRequestFactory;
    private readonly IHttpRequestFactory _cancelOrderRequestFactory;
    private readonly IHttpRequestFactory _cancelAllOrdersRequestFactory;
    private readonly UserStream _userStream;
    private readonly IRateLimiter _rateLimiter;
    private readonly ICompositeLoader<UserContext> _contextLoader;
    private readonly ICompositeLoader<IReadOnlyCollection<OrderModel>> _ordersLoader;
    private readonly IKeyedLoader<string, long, IReadOnlyCollection<TradeModel>> _tradesLoader;
    private readonly ISerializer<ReadOnlyMemory<byte>> _orderUpdateEventSerializer;

    public UserConnector(
        IServiceProvider sp,
        UserConfig config,
        QueryProcessor queryProcessor,
        SignatureService signatureService,
        [FromKeyedServices(SetLeverageKey)] IHttpRequestFactory setLeverageRequestFactory,
        [FromKeyedServices(InitOrderKey)] IHttpRequestFactory initOrderRequestFactory,
        [FromKeyedServices(ModifyOrderKey)] IHttpRequestFactory modifyOrderRequestFactory,
        [FromKeyedServices(CancelOrderKey)] IHttpRequestFactory cancelOrderRequestFactory,
        [FromKeyedServices(CancelAllOrdersKey)] IHttpRequestFactory cancelAllOrdersRequestFactory,
        UserStream userStream,
        ILoaderFactory loaderFactory,
        [FromKeyedServices(Provider)] IUserProvider userProvider,
        IRateLimiter rateLimiter,
        IStatusReporter reporter,
        IStatusMonitor monitor,
        ILogger logger
    )
        : base(config.GetSettings(), userProvider, reporter, monitor, logger)
    {
        _config = config;
        _queryProcessor = queryProcessor;
        _signatureService = signatureService;
        _setLeverageRequestFactory = setLeverageRequestFactory;
        _initOrderRequestFactory = initOrderRequestFactory;
        _modifyOrderRequestFactory = modifyOrderRequestFactory;
        _cancelOrderRequestFactory = cancelOrderRequestFactory;
        _cancelAllOrdersRequestFactory = cancelAllOrdersRequestFactory;

        // user stream
        _userStream = userStream;
        _rateLimiter = rateLimiter;
        _userStream.OnConnected += HandleConnected;
        Disposable += () => _userStream.OnConnected -= HandleConnected;

        _userStream.OnDisconnected += HandleDisconnected;
        Disposable += () => _userStream.OnConnected -= HandleDisconnected;

        _userStream.OnMessage += HandleMessage;
        Disposable += () => _userStream.OnMessage -= HandleMessage;

        _orderUpdateEventSerializer = sp.ResolveSerializer<ReadOnlyMemory<byte>>(OrderUpdateKey, Application.Json);

        // context
        Disposable += _contextLoader = loaderFactory.CreateCompositeLoader(_config.ReloadContext, LoadContextAsync);
        _contextLoader.OnData += HandleContext;
        Disposable += () => _contextLoader.OnData -= HandleContext;

        // orders
        Disposable += _ordersLoader = loaderFactory.CreateCompositeLoader(_config.ReloadOrders, LoadOrdersAsync);
        _ordersLoader.OnData += HandleOrders;
        Disposable += () => _ordersLoader.OnData -= HandleOrders;

        // deals
        Disposable += _tradesLoader = loaderFactory.CreateKeyedLoader<string, long, IReadOnlyCollection<TradeModel>>(
            _config.ReloadTrades,
            SystemClock.Instance.GetCurrentInstant().ToUnixTimeMilliseconds(),
            LoadTradesAsync,
            GetTradesContext
        );
        _tradesLoader.OnData += HandleTrades;
        Disposable += () => _tradesLoader.OnData -= HandleTrades;
    }

    public ValueTask InitAsync()
    {
        return ValueTask.CompletedTask;
    }

    public async Task<UserResult> SetLeverageAsync(PositionModel position, decimal leverage)
    {
        if (Status is not ConnectorStatus.Connected)
        {
            this.Warn("{id} skip for {position} -> {leverage} - not connected", Id, position, leverage);
            return UserResult.New(UserOperationStatus.NotConnected);
        }

        var result = await _setLeverageRequestFactory
            .New(_config.HttpApi)
            .Post("/fapi/v1/leverage")
            .Param("symbol", position.Symbol)
            .Param("leverage", leverage.FloorInt32())
            .ReceiveWindow()
            .Sign(_signatureService)
            .WithRateDelay1M(_rateLimiter)
            .WithLogFromWithHeaders(this, LogData.Headers | LogData.Response)
            .AsUserResultAsync<LeverageResponse>();

        HandleTradeResult(result.IsSuccess);

        return UserResult.Ok();
    }

    public async Task<UserResult<OrderModel?>> InitOrderAsync(IInitOrderRequest request)
    {
        if (Status is not ConnectorStatus.Connected)
        {
            this.Warn("{id} skip for {request} - not connected", Id, request);
            return UserResult.New(UserOperationStatus.NotConnected, default(OrderModel));
        }

        var queryResult = _queryProcessor.BuildInitOrderQuery(request);
        if (!queryResult.IsSuccess)
        {
            this.Warn("{id} query processing failed: {result}", Id, queryResult);
            return UserResult.From(queryResult, default(OrderModel));
        }

        var result = await _initOrderRequestFactory
            .New(_config.HttpApi)
            .Post("/fapi/v1/order")
            .Params(queryResult.Data)
            .ReceiveWindow()
            .Sign(_signatureService)
            .WithRateDelay1M(_rateLimiter)
            .WithLogFromWithHeaders(this, LogData.Headers | LogData.Response)
            .AsUserResultAsync<OrderModel>();

        HandleTradeResult(result.IsSuccess);

        return result;
    }

    public async Task<UserResult<OrderModel?>> ModifyOrderAsync(IModifyOrderRequest request)
    {
        if (Status is not ConnectorStatus.Connected)
        {
            this.Warn("{id} skip for {request} - not connected", Id, request);
            return UserResult.New(UserOperationStatus.NotConnected, default(OrderModel));
        }

        // non limit orders can only be canceled and created from scratch
        if (request.Order.Type is not OrderType.Limit)
        {
            // try cancel order
            var order = request.Order;
            var cancelRequest = RequestBuilder.CancelOrder(order.Id, order.ClientOrderId, order.Symbol);
            var cancelResult = await CancelOrderAsync(cancelRequest);
            if (cancelResult.IsFailure)
            {
                if (!cancelResult.IsAborted)
                    this.Warn("{id} cancel of order {order} failed: {result}", Id, order, cancelResult);
                return UserResult.From(cancelResult, default(OrderModel));
            }

            var initRequest = request.ToInitOrderRequest();
            var initResult = await InitOrderAsync(initRequest);

            return initResult;
        }

        var queryResult = _queryProcessor.BuildModifyOrderQuery(request);
        if (!queryResult.IsSuccess)
        {
            this.Warn("{id} query processing failed: {result}", Id, queryResult);
            return UserResult.From(queryResult, default(OrderModel));
        }

        var result = await _modifyOrderRequestFactory
            .New(_config.HttpApi)
            .Put("/fapi/v1/order")
            .Params(queryResult.Data)
            .ReceiveWindow()
            .Sign(_signatureService)
            .WithRateDelay1M(_rateLimiter)
            .WithLogFromWithHeaders(this, LogData.Headers | LogData.Response)
            .AsUserResultAsync<OrderModel>();

        HandleTradeResult(result.IsSuccess);

        return result;
    }

    public async Task<UserResult> CancelOrderAsync(ICancelOrderRequest request)
    {
        if (Status is not ConnectorStatus.Connected)
        {
            this.Warn("{id} skip for {order} - not connected", Id, request);
            return UserResult.New(UserOperationStatus.NotConnected);
        }

        var queryResult = _queryProcessor.BuildCancelOrderQuery(request);
        if (!queryResult.IsSuccess)
        {
            this.Warn("{id} query processing failed: {result}", Id, queryResult);
            return UserResult.From(queryResult);
        }

        var result = await _cancelOrderRequestFactory
            .New(_config.HttpApi)
            .Delete("/fapi/v1/order")
            .Params(queryResult.Data)
            .ReceiveWindow()
            .Sign(_signatureService)
            .WithRateDelay1M(_rateLimiter)
            .WithLogFromWithHeaders(this, LogData.Headers | LogData.Response)
            .AsUserResultAsync<CancelOrderResponse>();

        HandleTradeResult(result.IsSuccess);

        return UserResult.From(result);
    }

    public async Task<UserResult> CancelAllOrdersAsync(string symbol)
    {
        if (Status is not ConnectorStatus.Connected)
        {
            this.Warn<string, string>("{id} skip for {symbol} - not connected", Id, symbol);
            return UserResult.New(UserOperationStatus.NotConnected);
        }

        var queryResult = _queryProcessor.BuildCancelAllOrdersQuery(symbol);
        if (!queryResult.IsSuccess)
        {
            this.Warn("{id} query processing failed: {result}", Id, queryResult);
            return UserResult.From(queryResult);
        }

        var result = await _cancelAllOrdersRequestFactory
            .New(_config.HttpApi)
            .Delete("/fapi/v1/allOpenOrders")
            .Params(queryResult.Data)
            .ReceiveWindow()
            .Sign(_signatureService)
            .WithRateDelay1M(_rateLimiter)
            .WithLogFromWithHeaders(this, LogData.Headers | LogData.Response)
            .AsUserResultAsync<OperationResult>();

        HandleTradeResult(result.IsSuccess);

        return UserResult.From(result);
    }

    private void HandleTradeResult(bool isSuccess)
    {
        _contextLoader.Request();

        if (!isSuccess)
        {
            this.Trace<string>("{id} trade failed, request orders load", Id);
            _ordersLoader.Request();
        }
    }

    private async Task<IBaseResult<UserContext?>> LoadContextAsync(CancellationToken ct)
    {
        var result = await UserProvider.LoadContextAsync(Settings);

        return result;
    }

    private void HandleContext(UserContext context)
    {
        Write(ChangeEvent.Init(context.Assets));
        Write(ChangeEvent.Init(context.Positions));
    }

    private async Task<IBaseResult<IReadOnlyCollection<OrderModel>?>> LoadOrdersAsync(CancellationToken ct)
    {
        var result = await UserProvider.LoadOpenOrdersAsync(Settings);

        return result;
    }

    private void HandleOrders(IReadOnlyCollection<OrderModel> orders)
    {
        Write(ChangeEvent.Init(orders));
    }

    private async Task<IBaseResult<IReadOnlyCollection<TradeModel>?>> LoadTradesAsync(
        string symbol,
        long since,
        CancellationToken ct
    )
    {
        var result = await UserProvider.LoadTradesAsync(Settings, symbol, since);

        return result;
    }

    private long GetTradesContext(string symbol, long since, IReadOnlyCollection<TradeModel> trades)
    {
        var result = trades.Select(x => x.Moment).MaxBy(x => x);

        return result;
    }

    private void HandleTrades(string symbol, long since, IReadOnlyCollection<TradeModel> items)
    {
        foreach (var item in items)
            Write(item);
    }

    private void HandleConnected()
    {
        this.Trace<string>("{id} start", Id);

        _contextLoader.Start(true);
        _ordersLoader.Start(true);

        this.Trace<string>("{id} done", Id);
    }

    private void HandleDisconnected()
    {
        this.Trace<string>("{id} start", Id);

        _contextLoader.Stop();
        _ordersLoader.Stop();

        this.Trace<string>("{id} done", Id);
    }

    private void HandleMessage(ReadOnlyMemory<byte> data)
    {
        this.Trace<string, string>("{id} handle {msg}", Id, Encoding.UTF8.GetString(data.Span));
        // account info in event is almost useless (and position info lacks leverage value), so request account reload
        _contextLoader.Request();

        // handle order update
        var orderUpdate = _orderUpdateEventSerializer.Deserialize<OrderUpdateEvent?>(data);
        if (orderUpdate is not null)
            HandleOrderUpdate(orderUpdate);
    }

    private void HandleOrderUpdate(OrderUpdateEvent e)
    {
        if (e.Status is OrderStatus.PartiallyFilled or OrderStatus.Filled)
        {
            // as far as pnl is not available here - request reload by http
            _tradesLoader.Request(e.Symbol);
        }

        var order = new OrderModel(
            e.OrderId,
            e.ClientOrderId,
            e.Range,
            e.Symbol,
            e.Side,
            e.Type,
            e.TotalQty,
            e.Price,
            e.LevelPrice,
            e.ReduceOnly,
            e.CreatedAt,
            e.Status,
            e.ExecutedQty,
            e.ExecutedPrice,
            e.UpdatedAt
        );

        var item = order.Status is OrderStatus.New or OrderStatus.PartiallyFilled
            ? ChangeEvent.Set(order)
            : ChangeEvent.Delete(order);

        Write(item);
    }
}
