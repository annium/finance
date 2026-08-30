using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Annium.Data.Tables;
using Annium.Finance.Providers.Abstractions.Connectors.Shared;
using Annium.Finance.Providers.Abstractions.Connectors.User;
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
using Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.User.Contracts.Domain;
using Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.User.HttpExtensions;
using Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.User.Services;
using Annium.Logging;
using Annium.Net.Http;
using Annium.Serialization.Abstractions;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.User;

/// <summary>
/// Binance USD-M futures implementation of <see cref="IUserConnector"/>. Reloads account state (assets and
/// positions), open orders and trades through composite/keyed loaders, streams order updates from the user data
/// websocket, and sends leverage/order management commands over signed REST requests.
/// </summary>
internal class UserConnector : UserConnectorBase, IUserConnector
{
    /// <summary>The resolved user connector configuration.</summary>
    private readonly UserConfig _config;

    /// <summary>Builds the query parameters for order management requests.</summary>
    private readonly QueryProcessor _queryProcessor;

    /// <summary>Signs outgoing REST requests with the account's API secret.</summary>
    private readonly ISignatureService _signatureService;

    /// <summary>Factory for requests against the change-leverage endpoint.</summary>
    private readonly IHttpRequestFactory _setLeverageRequestFactory;

    /// <summary>Factory for requests against the place-order endpoint.</summary>
    private readonly IHttpRequestFactory _initOrderRequestFactory;

    /// <summary>Factory for requests against the modify-order endpoint.</summary>
    private readonly IHttpRequestFactory _modifyOrderRequestFactory;

    /// <summary>Factory for requests against the cancel-order endpoint.</summary>
    private readonly IHttpRequestFactory _cancelOrderRequestFactory;

    /// <summary>Factory for requests against the cancel-all-orders endpoint.</summary>
    private readonly IHttpRequestFactory _cancelAllOrdersRequestFactory;

    /// <summary>Limits request weight against the exchange's rate limits.</summary>
    private readonly IRateLimiter _rateLimiter;

    /// <summary>Reloads the account context (assets and positions).</summary>
    private readonly ICompositeLoader<UserContext> _contextLoader;

    /// <summary>Reloads the currently open orders.</summary>
    private readonly ICompositeLoader<IReadOnlyCollection<OrderModel>> _ordersLoader;

    /// <summary>Reloads trades for a symbol, keyed by symbol and the timestamp to load trades since.</summary>
    private readonly IKeyedLoader<string, long, IReadOnlyCollection<TradeModel>> _tradesLoader;

    /// <summary>The user data websocket stream.</summary>
    private readonly IUserStream _userStream;

    /// <summary>Deserializes <c>ORDER_TRADE_UPDATE</c> user data stream messages.</summary>
    private readonly ISerializer<ReadOnlyMemory<byte>> _orderUpdateEventSerializer;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserConnector"/> class, wiring the context/orders/trades
    /// loaders and the user data stream into the connector's lifecycle.
    /// </summary>
    /// <param name="config">The resolved user connector configuration.</param>
    /// <param name="provider">The user provider used to load account state, orders and trades.</param>
    /// <param name="queryProcessor">Builds the query parameters for order management requests.</param>
    /// <param name="signatureService">Signs outgoing REST requests.</param>
    /// <param name="setLeverageRequestFactory">Factory for requests against the change-leverage endpoint.</param>
    /// <param name="initOrderRequestFactory">Factory for requests against the place-order endpoint.</param>
    /// <param name="modifyOrderRequestFactory">Factory for requests against the modify-order endpoint.</param>
    /// <param name="cancelOrderRequestFactory">Factory for requests against the cancel-order endpoint.</param>
    /// <param name="cancelAllOrdersRequestFactory">Factory for requests against the cancel-all-orders endpoint.</param>
    /// <param name="rateLimiter">Limits request weight against the exchange's rate limits.</param>
    /// <param name="contextLoader">Loader that reloads the account context (assets and positions).</param>
    /// <param name="ordersLoader">Loader that reloads the currently open orders.</param>
    /// <param name="tradesLoader">Loader that reloads trades for a symbol.</param>
    /// <param name="userStream">The user data websocket stream.</param>
    /// <param name="orderUpdateEventSerializer">Deserializes <c>ORDER_TRADE_UPDATE</c> user data stream messages.</param>
    /// <param name="reporter">Reports connector status transitions.</param>
    /// <param name="monitor">Monitors connector status.</param>
    /// <param name="disposable">Accumulates cleanup actions for the connector's lifetime.</param>
    /// <param name="logger">The logger.</param>
    public UserConnector(
        UserConfig config,
        IUserProvider provider,
        QueryProcessor queryProcessor,
        ISignatureService signatureService,
        IHttpRequestFactory setLeverageRequestFactory,
        IHttpRequestFactory initOrderRequestFactory,
        IHttpRequestFactory modifyOrderRequestFactory,
        IHttpRequestFactory cancelOrderRequestFactory,
        IHttpRequestFactory cancelAllOrdersRequestFactory,
        IRateLimiter rateLimiter,
        ICompositeLoader<UserContext> contextLoader,
        ICompositeLoader<IReadOnlyCollection<OrderModel>> ordersLoader,
        IKeyedLoader<string, long, IReadOnlyCollection<TradeModel>> tradesLoader,
        IUserStream userStream,
        ISerializer<ReadOnlyMemory<byte>> orderUpdateEventSerializer,
        IStatusReporter reporter,
        IStatusMonitor monitor,
        AsyncDisposableBox disposable,
        ILogger logger
    )
        : base(config.GetSettings(), provider, reporter, monitor, disposable, logger)
    {
        _config = config;
        _queryProcessor = queryProcessor;
        _signatureService = signatureService;
        _setLeverageRequestFactory = setLeverageRequestFactory;
        _initOrderRequestFactory = initOrderRequestFactory;
        _modifyOrderRequestFactory = modifyOrderRequestFactory;
        _cancelOrderRequestFactory = cancelOrderRequestFactory;
        _cancelAllOrdersRequestFactory = cancelAllOrdersRequestFactory;
        _rateLimiter = rateLimiter;

        // context
        _contextLoader = contextLoader;
        _contextLoader.OnData += HandleContext;
        Disposable += () => _contextLoader.OnData -= HandleContext;

        // orders
        _ordersLoader = ordersLoader;
        _ordersLoader.OnData += HandleOrders;
        Disposable += () => _ordersLoader.OnData -= HandleOrders;

        // trades
        _tradesLoader = tradesLoader;
        _tradesLoader.OnData += HandleTrades;
        Disposable += () => _tradesLoader.OnData -= HandleTrades;

        // user stream
        _userStream = userStream;
        _userStream.OnConnected += HandleConnected;
        Disposable += () => _userStream.OnConnected -= HandleConnected;

        _userStream.OnDisconnected += HandleDisconnected;
        Disposable += () => _userStream.OnConnected -= HandleDisconnected;

        _userStream.OnMessage += HandleMessage;
        Disposable += () => _userStream.OnMessage -= HandleMessage;

        _orderUpdateEventSerializer = orderUpdateEventSerializer;
    }

    /// <summary>
    /// Sets the leverage used for a position, flooring the leverage to a whole number as required by the
    /// exchange. Always reports success to the caller (fire-and-forget over the account context reload).
    /// </summary>
    /// <param name="position">The position to change leverage for.</param>
    /// <param name="leverage">The leverage to set.</param>
    /// <returns>An OK result, or a not-connected failure if the connector is currently disconnected.</returns>
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

    /// <summary>
    /// Places a new order.
    /// </summary>
    /// <param name="request">The order parameters.</param>
    /// <returns>A result carrying the placed order on success, or null data with a non-success status on failure.</returns>
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

    /// <summary>
    /// Modifies an existing order. Binance's amend endpoint only supports limit orders; for any other order type
    /// this cancels the existing order and places a new one with the requested parameters instead.
    /// </summary>
    /// <param name="request">The modification parameters, including the order being modified.</param>
    /// <returns>A result carrying the resulting order on success, or null data with a non-success status on failure.</returns>
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

    /// <summary>
    /// Cancels an existing order.
    /// </summary>
    /// <param name="request">Identifies the order to cancel.</param>
    /// <returns>A result indicating whether the cancellation succeeded.</returns>
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

    /// <summary>
    /// Cancels all open orders for the given symbol.
    /// </summary>
    /// <param name="symbol">The instrument symbol to cancel orders for.</param>
    /// <returns>A result indicating whether the cancellation succeeded.</returns>
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

    /// <summary>
    /// Requests an account context reload after a trade command, and additionally requests an open orders reload
    /// if the command failed, since a failure may still have changed order state on the exchange.
    /// </summary>
    /// <param name="isSuccess">Whether the trade command succeeded.</param>
    private void HandleTradeResult(bool isSuccess)
    {
        _contextLoader.Request();

        if (!isSuccess)
        {
            this.Trace<string>("{id} trade failed, request orders load", Id);
            _ordersLoader.Request();
        }
    }

    /// <summary>
    /// Publishes the reloaded account context as full-snapshot <c>Init</c> events on <see cref="IUserConnector.Assets"/> and
    /// <see cref="IUserConnector.Positions"/>.
    /// </summary>
    /// <param name="context">The reloaded account context.</param>
    private void HandleContext(UserContext context)
    {
        Write(ChangeEvent.Init(context.Assets));
        Write(ChangeEvent.Init(context.Positions));
    }

    /// <summary>
    /// Publishes the reloaded open orders as a full-snapshot <c>Init</c> event on <see cref="IUserConnector.Orders"/>.
    /// </summary>
    /// <param name="orders">The reloaded open orders.</param>
    private void HandleOrders(IReadOnlyCollection<OrderModel> orders)
    {
        Write(ChangeEvent.Init(orders));
    }

    /// <summary>
    /// Publishes each reloaded trade individually on <see cref="IUserConnector.Trades"/>.
    /// </summary>
    /// <param name="symbol">The symbol trades were reloaded for.</param>
    /// <param name="since">The timestamp trades were reloaded since.</param>
    /// <param name="items">The reloaded trades.</param>
    private void HandleTrades(string symbol, long since, IReadOnlyCollection<TradeModel> items)
    {
        foreach (var item in items)
            Write(item);
    }

    /// <summary>
    /// Starts the account context and open orders loaders once the user data stream connects.
    /// </summary>
    private void HandleConnected()
    {
        this.Trace<string>("{id} start", Id);

        _contextLoader.Start(true);
        _ordersLoader.Start(true);

        this.Trace<string>("{id} done", Id);
    }

    /// <summary>
    /// Stops the account context and open orders loaders once the user data stream disconnects.
    /// </summary>
    private void HandleDisconnected()
    {
        this.Trace<string>("{id} start", Id);

        _contextLoader.Stop();
        _ordersLoader.Stop();

        this.Trace<string>("{id} done", Id);
    }

    /// <summary>
    /// Handles a raw user data stream message: since the <c>ACCOUNT_UPDATE</c> event carries an incomplete
    /// account snapshot (no leverage on positions), every message triggers an account context reload; messages
    /// that additionally parse as an <see cref="OrderUpdateEvent"/> are forwarded to <see cref="HandleOrderUpdate"/>.
    /// </summary>
    /// <param name="data">The raw message payload.</param>
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

    /// <summary>
    /// Publishes an <c>ORDER_TRADE_UPDATE</c> event on <see cref="IUserConnector.Orders"/> as a <c>Set</c> event while the order
    /// is still open (new or partially filled) and a <c>Delete</c> event once it stops being open. Also requests
    /// a trades reload for the symbol on a (partial) fill, since PnL is not available from the stream event.
    /// </summary>
    /// <param name="e">The order update event.</param>
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
