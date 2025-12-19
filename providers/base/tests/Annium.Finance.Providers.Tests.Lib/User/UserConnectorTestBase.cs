using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reactive.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Annium.Core.Mapper;
using Annium.Data.Tables;
using Annium.Extensions.Pooling;
using Annium.Finance.Providers.Abstractions.Connectors.Market;
using Annium.Finance.Providers.Abstractions.Connectors.Shared;
using Annium.Finance.Providers.Abstractions.Connectors.User;
using Annium.Finance.Providers.Abstractions.Domain.Market;
using Annium.Finance.Providers.Abstractions.Domain.User;
using Annium.Finance.Providers.Abstractions.Domain.User.Requests;
using Annium.Finance.Providers.Tests.Lib.User.Operations;
using Annium.Logging;
using Annium.Testing;
using Xunit;
using static Annium.Finance.Providers.Abstractions.Domain.User.Requests.RequestBuilder;

namespace Annium.Finance.Providers.Tests.Lib.User;

public abstract class UserConnectorTestBase : ProvidersTestBase, IAsyncLifetime
{
    protected InstrumentModel Instrument { get; private set; } = null!;
    protected string Symbol { get; }
    protected InstrumentTicker Ticker { get; private set; } = default!;
    private IUserConnector Connector { get; set; } = default!;
    private AsyncDisposableBox Disposable { get; set; }
    private readonly UserSettings _config;
    private readonly ConcurrentQueue<AssetModel> _assets = new();
    private readonly ConcurrentQueue<PositionModel> _positions = new();
    private readonly ConcurrentQueue<OrderModel> _orders = new();
    private readonly ConcurrentQueue<TradeModel> _trades = new();
    private readonly ConcurrentQueue<ConnectorError> _errors = new();
    private AssetModel _balance = default!;
    private PositionModel _position = default!;

    protected UserConnectorTestBase(UserSettings config, string symbol, ITestOutputHelper output)
        : base(output)
    {
        Disposable = Annium.Disposable.AsyncBox(Logger);
        _config = config;
        Symbol = symbol;
    }

    public async ValueTask InitializeAsync()
    {
        this.Trace("start");

        var marketConfig = Get<IMapper>().Map<MarketSettings>(_config);
        this.Trace("get market connector for {config}", marketConfig);
        var marketConnectorRef = await Get<IObjectCache<MarketSettings, IMarketConnector>>().GetAsync(marketConfig);
        Disposable += marketConnectorRef;
        var market = marketConnectorRef.Value;

        this.Trace("await until market connector is ready");
        await market.WhenConnectedAsync();

        this.Trace<string>("find instrument {symbol}", Symbol);
        Instrument = market.Instruments.Single(x => x.Symbol == Symbol);
        this.Trace("found instrument {instrument}", Instrument);

        this.Trace<string>("subscribe and wait for ticker for {symbol}", Symbol);
        market.SubscribeTickers([Symbol]);
        this.Trace<string>("find ticker for {symbol}", Symbol);
        Ticker = await market.Tickers.FirstAsync(x => x.Symbol == Symbol);
        this.Trace("found ticker for {instrument}", Instrument);

        this.Trace("get user connector for {config}", _config);
        var userConnectorRef = await Get<IObjectCache<UserSettings, IUserConnector>>().GetAsync(_config);
        Disposable += userConnectorRef;
        Connector = userConnectorRef.Value;

        this.Trace("subscribe to connector data");
        Disposable += Connector
            .Assets.Where(x => x.Type is ChangeEventType.Init)
            .SelectMany(x => x.Items)
            .Subscribe(_assets.Enqueue);
        Disposable += Connector
            .Positions.Where(x => x.Type is ChangeEventType.Init)
            .SelectMany(x => x.Items)
            .Subscribe(_positions.Enqueue);
        Disposable += Connector.Orders.Subscribe(x =>
        {
            if (x.Type is ChangeEventType.Init)
                foreach (var item in x.Items)
                    _orders.Enqueue(item);
            else
                _orders.Enqueue(x.Item);
        });
        Disposable += Connector.Trades.Subscribe(_trades.Enqueue);

        this.Trace("subscribe to connector errors");
        Connector.OnError += _errors.Enqueue;

        this.Trace("await until user connector is ready");
        await Connector.WhenConnectedAsync();

        this.Trace("cancel open orders");
        await CancelOpenOrders();

        this.Trace("await for balances");
        await AwaitForInitialBalances();

        this.Trace("await for positions and leverages (before closing)");
        await AwaitForInitialPositionsAndLeverages();

        this.Trace("close active positions");
        await CloseActivePositions();

        EnsureNoErrors();

        this.Trace("done");
    }

    public async ValueTask DisposeAsync()
    {
        this.Trace("start");

        this.Trace("cancel open orders");
        await CancelOpenOrders();

        this.Trace("try close position if any");
        var amount = GetPositionAmount();
        if (amount > 0)
        {
            this.Trace("close position amount: {amount}", amount);
            await InitValidOrder(
                InitMarketOrder(ClientOrderId(), Range(), Symbol, OrderSide.Sell, amount),
                OrderStatus.Filled
            );
            await EnsureBalanceIsIncreased();
            await EnsurePositionIsDecreased();
        }

        EnsureNoErrors();

        this.Trace("dispose disposables");
        await Disposable.DisposeAsync();

        EnsureNoErrors();

        this.Trace("done");
    }

    protected void EnsureNoErrors()
    {
        _errors.IsEmpty();
    }

    protected void Snapshot()
    {
        this.Trace("start");

        _balance = GetBalance(Instrument.Currency.Code);
        _position = GetPosition();

        this.Trace("done");
    }

    private async Task CloseActivePositions()
    {
        this.Trace("start");

        var activePositions = _positions
            .DistinctBy(x => new { x.Symbol, x.OrientationRange })
            .Where(x => x.Amount > 0)
            .ToArray();

        if (activePositions.Length == 0)
        {
            this.Trace("no active positions, break");
            return;
        }

        foreach (var position in activePositions)
        {
            this.Trace("close {instrument} position with amount {amount}", Instrument, position.Amount);
            await Connector
                .InitOrderAsync(
                    InitMarketOrder(
                        ClientOrderId(),
                        Range(),
                        Symbol,
                        position.Amount < 0 ? OrderSide.Buy : OrderSide.Sell,
                        Math.Abs(position.Amount)
                    )
                )
                .UnwrapAsync();
        }

        EnsureNoErrors();

        this.Trace("close active positions - done");
    }

    protected async Task InitInvalidOrder(IInitOrderRequest request)
    {
        this.Trace("start");

        await Connector.InitOrderAsync(request).EnsureFailedAsync();

        EnsureNoErrors();

        this.Trace("done");
    }

    protected async Task<OrderModel> InitValidOrder(IInitOrderRequest request, OrderStatus status)
    {
        this.Trace("start");

        Snapshot();

        // act
        this.Trace("execute start");
        var order = await Connector.InitOrderAsync(request).UnwrapAsync();
        this.Trace("execute done");

        EnsureNoErrors();

        // assert
        order.ShouldMatch(request);
        await EnsureOrderReported(order, status);

        EnsureNoErrors();

        this.Trace("done");

        return order;
    }

    protected async Task CancelInvalidOrder(OrderModel order)
    {
        this.Trace("start");

        var request = CancelOrder(order.Id, order.ClientOrderId, order.Symbol);
        await Connector.CancelOrderAsync(request).EnsureFailedAsync();

        EnsureNoErrors();

        this.Trace("done");
    }

    protected async Task CancelValidOrder(OrderModel order)
    {
        this.Trace("start");

        Snapshot();

        // cleanup
        this.Trace("execute start");
        var request = CancelOrder(order.Id, order.ClientOrderId, order.Symbol);
        await Connector.CancelOrderAsync(request).UnwrapAsync();
        this.Trace("execute done");

        EnsureNoErrors();

        // assert
        await EnsureOrderReported(order, OrderStatus.Canceled);

        EnsureNoErrors();

        this.Trace("done");
    }

    protected async Task ModifyInvalidOrder(IModifyOrderRequest request)
    {
        this.Trace("start");

        await Connector.ModifyOrderAsync(request).EnsureFailedAsync();

        EnsureNoErrors();

        this.Trace("done");
    }

    protected async Task<OrderModel> ModifyValidOrder(IModifyOrderRequest request, OrderStatus status)
    {
        this.Trace("start");

        Snapshot();

        // act
        this.Trace("execute start");
        var order = await Connector.ModifyOrderAsync(request).UnwrapAsync();
        this.Trace("execute done");

        EnsureNoErrors();

        // assert
        order.ShouldMatch(request);
        await EnsureOrderReported(order, status);

        EnsureNoErrors();

        this.Trace("done");

        return order;
    }

    protected async Task CancelOpenOrders()
    {
        this.Trace("cancel all orders - start");

        // cancel existing orders
        await Connector.CancelAllOrdersAsync(Symbol).UnwrapAsync();

        EnsureNoErrors();

        this.Trace("cancel all orders - done");
    }

    protected async Task AwaitForInitialBalances()
    {
        // await until balances arrive and a second more before starting test
        this.Trace("await for balances");
        await Expect.ToAsync(() => _assets.IsNotEmpty());
        await WaitForMessages();
    }

    protected Task AwaitForInitialPositionsAndLeverages()
    {
        this.Trace("await for positions");
        return Expect.ToAsync(() => _positions.IsNotEmpty());
    }

    protected decimal GetPositionAmount()
    {
        this.Trace<string>("get size of {symbol} position", Symbol);
        var amount = GetPosition().Amount;

        return Instrument.ToLotSize(amount);
    }

    protected string ClientOrderId() => Guid.NewGuid().ToString();

    protected OrientationRange Range() => OrientationRange.Both;

    protected AssetModel GetBalance(string resource)
    {
        this.Trace<string>("get {resource} last balance", resource);
        var asset = _assets.Last(x => x.Resource == resource);
        this.Trace("got {resource} last balance: {asset}", resource, asset);

        return asset;
    }

    private PositionModel GetPosition()
    {
        this.Trace("get {instrument} position", Instrument);
        var position = _positions.Last(x => x.OrientationRange is OrientationRange.Both && x.Symbol == Symbol);
        this.Trace("got {instrument} last position: {position}", Instrument, position);

        return position;
    }

    protected Task EnsureBalanceIsLocked()
    {
        var originalBalance = _balance;

        this.Trace<string>(
            "ensure current balance is locked compared to original {balance}",
            JsonSerializer.Serialize(originalBalance)
        );

        return Expect.ToAsync(() =>
        {
            var currentBalance = GetBalance(Instrument.Currency.Code);
            currentBalance.Free.IsLess(originalBalance.Free);
            currentBalance.Locked.IsGreater(originalBalance.Locked);
        });
    }

    protected Task EnsureBalanceIsReleased()
    {
        var originalBalance = _balance;

        this.Trace<string>(
            "ensure current balance is released compared to original {balance}",
            JsonSerializer.Serialize(originalBalance)
        );

        return Expect.ToAsync(() =>
        {
            var currentBalance = GetBalance(Instrument.Currency.Code);
            currentBalance.Free.IsGreater(originalBalance.Free);
            currentBalance.Locked.IsLess(originalBalance.Locked);
        });
    }

    protected Task EnsureBalanceIsIncreased()
    {
        var originalBalance = _balance;

        this.Trace<string>(
            "ensure current free balance is greater than original {balance}",
            JsonSerializer.Serialize(originalBalance)
        );

        return Expect.ToAsync(() =>
        {
            var currentBalance = GetBalance(Instrument.Currency.Code);
            currentBalance.Free.IsGreater(originalBalance.Free);
        });
    }

    protected Task EnsureBalanceIsDecreased()
    {
        var originalBalance = _balance;

        this.Trace<string>(
            "ensure current free balance is smaller than original {balance}",
            JsonSerializer.Serialize(originalBalance)
        );

        return Expect.ToAsync(() =>
        {
            var currentBalance = GetBalance(Instrument.Currency.Code);
            currentBalance.Free.IsLess(originalBalance.Free);
        });
    }

    protected Task EnsurePositionIsIncreased()
    {
        var originalPosition = _position;

        this.Trace<string>(
            "ensure position amount is increased compared to original {0}",
            JsonSerializer.Serialize(originalPosition)
        );

        return Expect.ToAsync(() =>
        {
            var currentPosition = GetPosition();
            currentPosition.Amount.IsGreater(originalPosition.Amount);
        });
    }

    protected Task EnsurePositionIsDecreased()
    {
        var originalPosition = _position;

        this.Trace<string>(
            "ensure position amount is decreased compared to original {0}",
            JsonSerializer.Serialize(originalPosition)
        );

        return Expect.ToAsync(() =>
        {
            var currentPosition = GetPosition();
            currentPosition.Amount.IsLess(originalPosition.Amount);
        });
    }

    private Task EnsureOrderReported(OrderModel order, OrderStatus status)
    {
        this.Trace("ensure order {order} is reported and has status {status}", order.Id, status);
        return Expect.ToAsync(() =>
        {
            var orderMessage = _orders.Last(x => x.Id == order.Id);
            orderMessage.ShouldMatch(order);
            orderMessage.Status.Is(status);
        });
    }

    private Task WaitForMessages()
    {
        this.Trace("await for messages");
        return Task.Delay(1000);
    }
}
