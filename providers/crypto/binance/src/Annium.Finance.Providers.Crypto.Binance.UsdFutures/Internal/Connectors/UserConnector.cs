using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Annium.Core.DependencyInjection;
using Annium.Data.Tables;
using Annium.Finance.Providers.Abstractions.Connectors.Connectors;
using Annium.Finance.Providers.Abstractions.Domain.Dto;
using Annium.Finance.Providers.Abstractions.Domain.Enums;
using Annium.Finance.Providers.Abstractions.Domain.Extensions;
using Annium.Finance.Providers.Abstractions.Domain.Interfaces;
using Annium.Finance.Providers.Abstractions.Domain.Operations;
using Annium.Finance.Providers.Crypto.Binance.Base;
using Annium.Finance.Providers.Crypto.Binance.Base.Connectors;
using Annium.Finance.Providers.Crypto.Binance.Base.Connectors.Extensions;
using Annium.Finance.Providers.Crypto.Binance.Base.Contracts.Shared.Domain;
using Annium.Finance.Providers.Crypto.Binance.Base.Contracts.User.Domain;
using Annium.Finance.Providers.Crypto.Binance.Base.Services;
using Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Connectors.Extensions;
using Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Contracts.User.Domain;
using Annium.Finance.Providers.Shared.Connectors;
using Annium.Finance.Providers.Shared.Services;
using Annium.Logging;
using Annium.Net.Http;
using Annium.Serialization.Abstractions;
using Annium.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using static Annium.Finance.Providers.Crypto.Binance.UsdFutures.Constants;
using static System.Net.Mime.MediaTypeNames;

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
    private readonly ICompositeLoader<AccountResponse> _accountLoader;
    private readonly IHttpRequestFactory _getAccountRequestFactory;
    private readonly ICompositeLoader<IReadOnlyCollection<OrderDto>> _ordersLoader;
    private readonly IHttpRequestFactory _getOrderRequestFactory;
    private readonly IKeyedLoader<string, long, IReadOnlyCollection<TradeResponse>> _tradesLoader;
    private readonly IHttpRequestFactory _getTradeRequestFactory;
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
        [FromKeyedServices(GetAccountKey)] IHttpRequestFactory getAccountRequestFactory,
        [FromKeyedServices(GetOrderKey)] IHttpRequestFactory getOrderRequestFactory,
        [FromKeyedServices(GetTradeKey)] IHttpRequestFactory getTradeRequestFactory,
        ILoaderFactory loaderFactory,
        [FromKeyedServices(Provider)] IUserProvider userProvider,
        IStatusMonitor monitor,
        ILogger logger
    )
        : base(config.GetSettings(), userProvider, monitor, logger)
    {
        _config = config;
        _queryProcessor = queryProcessor;
        _signatureService = signatureService;
        _setLeverageRequestFactory = setLeverageRequestFactory;
        _initOrderRequestFactory = initOrderRequestFactory;
        _modifyOrderRequestFactory = modifyOrderRequestFactory;
        _cancelOrderRequestFactory = cancelOrderRequestFactory;
        _getAccountRequestFactory = getAccountRequestFactory;
        _getOrderRequestFactory = getOrderRequestFactory;
        _getTradeRequestFactory = getTradeRequestFactory;
        _cancelAllOrdersRequestFactory = cancelAllOrdersRequestFactory;

        // user stream
        _userStream = userStream;
        _userStream.OnConnected += HandleConnected;
        Disposable += () => _userStream.OnConnected -= HandleConnected;

        _userStream.OnDisconnected += HandleDisconnected;
        Disposable += () => _userStream.OnConnected -= HandleDisconnected;

        _userStream.OnMessage += HandleMessage;
        Disposable += () => _userStream.OnMessage -= HandleMessage;

        _orderUpdateEventSerializer = sp.ResolveKeyed<ISerializer<ReadOnlyMemory<byte>>>(
            SerializerKey.Create(OrderUpdateKey, Application.Json)
        );

        // accounts
        Disposable += _accountLoader = loaderFactory.CreateCompositeLoader(_config.ReloadAccount, LoadAccount);
        _accountLoader.OnData += HandleAccount;
        Disposable += () => _accountLoader.OnData -= HandleAccount;

        // orders
        Disposable += _ordersLoader = loaderFactory.CreateCompositeLoader(_config.ReloadOrders, LoadOrders);
        _ordersLoader.OnData += HandleOrders;
        Disposable += () => _ordersLoader.OnData -= HandleOrders;

        // deals
        Disposable += _tradesLoader = loaderFactory.CreateKeyedLoader<string, long, IReadOnlyCollection<TradeResponse>>(
            _config.ReloadTrades,
            SystemClock.Instance.GetCurrentInstant().ToUnixTimeMilliseconds(),
            LoadTrades,
            GetTradesContext
        );
        _tradesLoader.OnData += HandleTrades;
        Disposable += () => _tradesLoader.OnData -= HandleTrades;
    }

    public ValueTask InitAsync()
    {
        return ValueTask.CompletedTask;
    }

    public async Task<UserResult> SetLeverage(PositionDto position, decimal leverage)
    {
        if (Status is not ConnectorStatus.Connected)
        {
            this.Trace("skip for {position} -> {leverage} - not connected", position, leverage);
            return UserResult.New(UserOperationStatus.NotConnected);
        }

        this.Trace("send request");
        var result = await _setLeverageRequestFactory
            .New(_config.HttpApi)
            .Post("/fapi/v1/leverage")
            .Param("symbol", position.Symbol)
            .Param("leverage", leverage.FloorInt32())
            .ReceiveWindow()
            .Sign(_signatureService)
            .WithRateDelay1M()
            .WithLogFrom(this, LogData.Headers | LogData.Response)
            .AsUserResultAsync<LeverageResponse>();

        HandleTradeResult(result.IsSuccess);

        this.Trace("done");

        return UserResult.Ok();
    }

    public async Task<UserResult<OrderDto?>> InitOrder(IInitOrderRequest request)
    {
        if (Status is not ConnectorStatus.Connected)
        {
            this.Trace("skip for {request} - not connected", request);
            return UserResult.New(UserOperationStatus.NotConnected, default(OrderDto));
        }

        var queryResult = _queryProcessor.BuildInitOrderQuery(request);
        if (queryResult.IsFailure)
        {
            this.Trace("query processing failed: {result}", queryResult);
            return UserResult.From(queryResult, default(OrderDto));
        }

        this.Trace("send request");
        var result = await _initOrderRequestFactory
            .New(_config.HttpApi)
            .Post("/fapi/v1/order")
            .Params(queryResult.Data)
            .ReceiveWindow()
            .Sign(_signatureService)
            .WithRateDelay1M()
            .WithLogFrom(this, LogData.Headers | LogData.Response)
            .AsUserResultAsync<OrderDto>();

        HandleTradeResult(result.IsSuccess);

        this.Trace("done");

        return result;
    }

    public async Task<UserResult<OrderDto?>> ModifyOrder(IModifyOrderRequest request)
    {
        if (Status is not ConnectorStatus.Connected)
        {
            this.Trace("skip for {request} - not connected", request);
            return UserResult.New(UserOperationStatus.NotConnected, default(OrderDto));
        }

        // non limit orders can only be canceled and created from scratch
        if (request.Order.Type is not OrderType.Limit)
        {
            // try cancel order
            this.Trace("try cancel order {order}", request.Order);
            var cancelResult = await CancelOrder(request.Order);
            if (cancelResult.IsFailure)
            {
                this.Trace("cancel of order {order} failed: {result}", request.Order, cancelResult);
                return UserResult.From(cancelResult, default(OrderDto));
            }

            var initRequest = request.ToInitOrderRequest();
            this.Trace("init new order {request}", initRequest);
            var initResult = await InitOrder(initRequest);

            return initResult;
        }

        var queryResult = _queryProcessor.BuildModifyOrderQuery(request);
        if (queryResult.IsFailure)
        {
            this.Trace("query processing failed: {result}", queryResult);
            return UserResult.From(queryResult, default(OrderDto));
        }

        this.Trace("send request");
        var result = await _modifyOrderRequestFactory
            .New(_config.HttpApi)
            .Put("/fapi/v1/order")
            .Params(queryResult.Data)
            .ReceiveWindow()
            .Sign(_signatureService)
            .WithRateDelay1M()
            .WithLogFrom(this, LogData.Headers | LogData.Response)
            .AsUserResultAsync<OrderDto>();

        HandleTradeResult(result.IsSuccess);

        this.Trace("done");

        return result;
    }

    public async Task<UserResult> CancelOrder(OrderDto order)
    {
        if (Status is not ConnectorStatus.Connected)
        {
            this.Trace("skip for {order} - not connected", order);
            return UserResult.New(UserOperationStatus.NotConnected);
        }

        var queryResult = _queryProcessor.BuildCancelOrderQuery(order);
        if (queryResult.IsFailure)
        {
            this.Trace("query processing failed: {result}", queryResult);
            return UserResult.From(queryResult);
        }

        this.Trace("send request");
        var result = await _cancelOrderRequestFactory
            .New(_config.HttpApi)
            .Delete("/fapi/v1/order")
            .Params(queryResult.Data)
            .ReceiveWindow()
            .Sign(_signatureService)
            .WithRateDelay1M()
            .WithLogFrom(this, LogData.Headers | LogData.Response)
            .AsUserResultAsync<CancelOrderResponse>();

        HandleTradeResult(result.IsSuccess);

        this.Trace("done");

        return UserResult.From(result);
    }

    public async Task<UserResult> CancelAllOrders(string symbol)
    {
        if (Status is not ConnectorStatus.Connected)
        {
            this.Trace<string>("skip for {symbol} - not connected", symbol);
            return UserResult.New(UserOperationStatus.NotConnected);
        }

        var queryResult = _queryProcessor.BuildCancelAllOrdersQuery(symbol);
        if (queryResult.IsFailure)
        {
            this.Trace("query processing failed: {result}", queryResult);
            return UserResult.From(queryResult);
        }

        this.Trace("send request");
        var result = await _cancelAllOrdersRequestFactory
            .New(_config.HttpApi)
            .Delete("/fapi/v1/allOpenOrders")
            .Params(queryResult.Data)
            .ReceiveWindow()
            .Sign(_signatureService)
            .WithRateDelay1M()
            .WithLogFrom(this, LogData.Headers | LogData.Response)
            .AsUserResultAsync<OperationResult>();

        HandleTradeResult(result.IsSuccess);

        this.Trace("done");

        return UserResult.From(result);
    }

    private void HandleTradeResult(bool isSuccess)
    {
        this.Trace("request account load");
        _accountLoader.Request();

        if (!isSuccess)
        {
            this.Trace("request orders load");
            _ordersLoader.Request();
        }
    }

    private async Task<IBaseResult<AccountResponse?>> LoadAccount(CancellationToken ct)
    {
        this.Trace("start");
        var result = await _getAccountRequestFactory
            .New(_config.HttpApi)
            .Get("/fapi/v2/account")
            .ReceiveWindow()
            .Sign(_signatureService)
            .WithRateDelay1M()
            .WithLogFrom(this, LogData.Headers | LogData.Response)
            .AsUserResultAsync<AccountResponse>();
        this.Trace("done");

        return result;
    }

    private void HandleAccount(AccountResponse response)
    {
        this.Trace("start");

        var assets = response.Balances
            .Select(x => new AssetDto(x.Asset, x.Free, x.InitialMargin + x.MaintenanceMargin))
            .ToArray();
        AssetWriter.Write(ChangeEvent.Init(assets));

        var positions = response.Positions
            .Select(x => new PositionDto(x.Symbol, x.Orientation, x.MarginType, x.Leverage, x.Amount))
            .ToArray();
        PositionWriter.Write(ChangeEvent.Init(positions));

        this.Trace("done");
    }

    private async Task<IBaseResult<IReadOnlyCollection<OrderDto>?>> LoadOrders(CancellationToken ct)
    {
        this.Trace("start");
        var result = await _getOrderRequestFactory
            .New(_config.HttpApi)
            .Get("/fapi/v1/openOrders")
            .ReceiveWindow()
            .Sign(_signatureService)
            .WithRateDelay1M()
            .WithLogFrom(this, LogData.Headers | LogData.Response)
            .AsUserResultAsync<IReadOnlyCollection<OrderDto>>();
        this.Trace("done");

        return result;
    }

    private void HandleOrders(IReadOnlyCollection<OrderDto> orders)
    {
        this.Trace("start");

        OrderWriter.Write(ChangeEvent.Init(orders));

        this.Trace("done");
    }

    private async Task<IBaseResult<IReadOnlyCollection<TradeResponse>?>> LoadTrades(
        string symbol,
        long since,
        CancellationToken ct
    )
    {
        this.Trace("start");
        var result = await _getTradeRequestFactory
            .New(_config.HttpApi)
            .Get("/fapi/v1/userTrades")
            .Param("symbol", symbol)
            .Param("startTime", since)
            .Param("limit", 1000)
            .ReceiveWindow()
            .Sign(_signatureService)
            .WithRateDelay1M()
            .WithLogFrom(this, LogData.Headers | LogData.Response)
            .AsUserResultAsync<IReadOnlyCollection<TradeResponse>>();
        this.Trace("done");

        return result;
    }

    private long GetTradesContext(string symbol, long since, IReadOnlyCollection<TradeResponse> trades)
    {
        this.Trace("start");
        var result = trades.Select(x => x.Moment).MaxBy(x => x);
        this.Trace("done");

        return result;
    }

    private void HandleTrades(string symbol, long since, IReadOnlyCollection<TradeResponse> items)
    {
        this.Trace("start");

        foreach (var item in items)
        {
            var trade = new TradeDto(
                item.Id,
                item.OrderId,
                item.Symbol,
                item.Price,
                item.Qty,
                item.CommissionAsset,
                item.Commission,
                item.Maker,
                item.Moment
            );
            TradeWriter.Write(trade);
        }

        this.Trace("done");
    }

    private void HandleConnected()
    {
        this.Trace("start");

        _accountLoader.Start();
        _ordersLoader.Start();

        this.Trace("done");
    }

    private void HandleDisconnected()
    {
        this.Trace("start");

        _accountLoader.Stop();
        _ordersLoader.Stop();

        this.Trace("done");
    }

    private void HandleMessage(ReadOnlyMemory<byte> data)
    {
        this.Trace("start");

        // account info in event is almost useless (and position info lacks leverage value), so request account reload
        _accountLoader.Request();

        // handle order update
        var orderUpdate = _orderUpdateEventSerializer.Deserialize<OrderUpdateEvent?>(data);
        if (orderUpdate is not null)
            HandleOrderUpdate(orderUpdate);
    }

    private void HandleOrderUpdate(OrderUpdateEvent e)
    {
        this.Trace("start");

        if (e.Status is OrderStatus.PartiallyFilled or OrderStatus.Filled)
        {
            // as far as pnl is not available here - request reload by http
            _tradesLoader.Request(e.Symbol);
        }

        var order = new OrderDto(
            e.OrderId,
            e.ClientOrderId,
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

        OrderWriter.Write(item);

        this.Trace("done");
    }
}
