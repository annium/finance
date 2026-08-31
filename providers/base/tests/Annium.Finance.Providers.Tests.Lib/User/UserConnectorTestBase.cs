using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reactive.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Annium.Core.Mapper;
using Annium.Data.Tables;
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

/// <summary>
/// Base for tests that drive a provider's live user connector against a real exchange account. This is not
/// a sandbox: derived tests place, fill, cancel and modify real orders on the account identified by the
/// <see cref="UserSettings"/> passed to the constructor, and this base actively manages that account's state
/// around each test - on setup it cancels every open order on <see cref="Symbol"/> and closes any active
/// position on it, and on teardown it cancels open orders again and market-sells off any remaining position,
/// so tests start and leave the account flat. Gate real runs behind <see cref="Exchange.IsEnabled"/>.
/// </summary>
/// <remarks>
/// One provider derives from this: the Binance USD-M futures suite. Spot has no user-connector tests at all -
/// only provider ones - so nothing exercises a spot user connector, gated or otherwise. Two commented-out
/// bases used to stand in for that coverage, one of them carrying a position-closing filter that skipped
/// short positions, which is a defect already fixed here; they were removed rather than left as something to
/// revive. The coverage they implied still does not exist.
/// </remarks>
public abstract class UserConnectorTestBase : ProvidersTestBase, IAsyncLifetime
{
    /// <summary>Gets the instrument metadata resolved for <see cref="Symbol"/> from the market connector.</summary>
    protected InstrumentModel Instrument { get; private set; } = null!;

    /// <summary>Gets the symbol the derived test drives the connector scenario for.</summary>
    protected string Symbol { get; }

    /// <summary>Gets the latest ticker resolved for <see cref="Symbol"/> from the market connector.</summary>
    protected InstrumentTicker Ticker { get; private set; } = null!;

    /// <summary>Gets the live user connector under test.</summary>
    private IUserConnector Connector { get; set; } = null!;

    /// <summary>The account credentials/environment the connector authenticates with.</summary>
    private readonly UserSettings _settings;

    /// <summary>Every asset balance update the connector has reported so far.</summary>
    private readonly ConcurrentQueue<AssetModel> _assets = new();

    /// <summary>Every position update the connector has reported so far.</summary>
    private readonly ConcurrentQueue<PositionModel> _positions = new();

    /// <summary>Every order update the connector has reported so far.</summary>
    private readonly ConcurrentQueue<OrderModel> _orders = new();

    /// <summary>Every trade the connector has reported so far.</summary>
    private readonly ConcurrentQueue<TradeModel> _trades = new();

    /// <summary>Every error the connector has raised so far.</summary>
    private readonly ConcurrentQueue<ConnectorError> _errors = new();

    /// <summary>The balance of the instrument's currency at the last <see cref="Snapshot"/>.</summary>
    private AssetModel _balance = null!;

    /// <summary>The position on <see cref="Symbol"/> at the last <see cref="Snapshot"/>.</summary>
    private PositionModel _position = null!;

    /// <summary>Collects the connector and its subscriptions so they can be disposed together.</summary>
    private AsyncDisposableBox _disposable = null!;

    /// <summary>Whether <see cref="InitializeAsync"/> ran all the way through, so teardown can rely on it.</summary>
    private bool _isInitialized;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserConnectorTestBase"/> class.
    /// </summary>
    /// <param name="settings">The account credentials/environment the connector authenticates with.</param>
    /// <param name="symbol">The symbol to drive the connector scenario for.</param>
    /// <param name="output">The xUnit output helper to route trace logging to.</param>
    protected UserConnectorTestBase(UserSettings settings, string symbol, ITestOutputHelper output)
        : base(output)
    {
        _settings = settings;
        Symbol = symbol;
    }

    /// <summary>
    /// Connects a market connector to resolve <see cref="Instrument"/> and <see cref="Ticker"/>, then connects
    /// the user connector under test, subscribes to its data and errors, cancels any open orders on
    /// <see cref="Symbol"/>, waits for the initial balance and position snapshot to arrive, and closes any
    /// active position - so every test starts from a flat account with no open orders.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public override async ValueTask InitializeAsync()
    {
        // the base builds the provider; everything below resolves from it - the logger included - so it
        // has to run first. A constructor is too early for anything that comes out of the container
        await base.InitializeAsync();

        _disposable = Disposable.AsyncBox(Logger);

        this.Trace("start");

        // arrange - market
        this.Trace("get market connector factory");
        var marketFactory = Get<IMarketConnectorFactory>();

        var settings = Get<IMapper>().Map<MarketSettings>(_settings);
        this.Trace("get market connector for {settings}", settings);
        var market = marketFactory.Create(settings);
        _disposable += market;

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

        // arrange - user
        this.Trace("get user connector factory");
        var userFactory = Get<IUserConnectorFactory>();

        this.Trace("get user connector for {settings}", _settings);
        _disposable += Connector = userFactory.Create(_settings);

        // every queue takes the initial snapshot AND the updates that follow it. Keeping only the Init
        // event, as assets and positions used to, froze both queues at the account's opening state:
        // GetBalance and GetPosition then answered with the same snapshot for the rest of the test, so
        // the Ensure*Increased/Decreased checks below were comparing a value against itself
        this.Trace("subscribe to connector data");
        _disposable += Connector.Assets.Subscribe(x =>
        {
            if (x.Type is ChangeEventType.Init)
                foreach (var item in x.Items)
                    _assets.Enqueue(item);
            else
                _assets.Enqueue(x.Item);
        });
        _disposable += Connector.Positions.Subscribe(x =>
        {
            if (x.Type is ChangeEventType.Init)
                foreach (var item in x.Items)
                    _positions.Enqueue(item);
            else
                _positions.Enqueue(x.Item);
        });
        _disposable += Connector.Orders.Subscribe(x =>
        {
            if (x.Type is ChangeEventType.Init)
                foreach (var item in x.Items)
                    _orders.Enqueue(item);
            else
                _orders.Enqueue(x.Item);
        });
        _disposable += Connector.Trades.Subscribe(_trades.Enqueue);

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

        // only now: everything teardown's account cleanup reads - the connector, and the queues holding
        // the opening balance and position - is in place. Set before this, it would promise teardown a
        // fixture that a failure part-way through had not finished building
        _isInitialized = true;

        this.Trace("done");
    }

    /// <summary>
    /// Cancels any open orders on <see cref="Symbol"/>, market-sells off any remaining position so the
    /// account is left flat, then disposes the connector and its subscriptions.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public override async ValueTask DisposeAsync()
    {
        this.Trace("start");

        // the cleanup below and the disposal after it are separate obligations, and the first must not be
        // able to cancel the second. Initialization can fail with a live market connector already in the
        // box - a symbol the exchange does not list is enough - and it leaves the connector unbuilt and the
        // snapshot queues empty, so cleanup would throw on its first line. Thrown from here that ended the
        // whole method: the connector stayed connected and the container behind it was never released
        try
        {
            if (_isInitialized)
                await CleanUpAccountAsync();
            else
                this.Trace("skip account cleanup, initialization did not complete");
        }
        finally
        {
            this.Trace("dispose disposables");
            if (_disposable is not null)
                await _disposable.DisposeAsync();

            // the connector and its subscriptions are gone; the container that built them is the base's to
            // release. InitializeAsync calls up to the base, and this has to match it or the provider - and
            // every singleton in it - outlives every test class that ever ran
            await base.DisposeAsync();
        }

        EnsureNoErrors();

        this.Trace("done");
    }

    /// <summary>
    /// Leaves the account as this fixture found it: every open order on <see cref="Symbol"/> cancelled and
    /// any remaining position flattened.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task CleanUpAccountAsync()
    {
        this.Trace("cancel open orders");
        await CancelOpenOrders();

        this.Trace("try close position if any");
        var amount = GetPositionAmount();
        if (amount != 0)
        {
            this.Trace("close position amount: {amount}", amount);
            // close in whichever direction flattens it: selling off a short would deepen it
            await InitValidOrder(
                InitMarketOrder(
                    ClientOrderId(),
                    Range(),
                    Symbol,
                    amount < 0 ? OrderSide.Buy : OrderSide.Sell,
                    Math.Abs(amount)
                ),
                OrderStatus.Filled
            );
            await EnsureBalanceIsIncreased();
            // flattening moves the amount towards zero, which is downwards for a long and upwards for
            // a short - asserting "decreased" either way would fail on every short that ever closed
            if (amount < 0)
                await EnsurePositionIsIncreased();
            else
                await EnsurePositionIsDecreased();
        }

        EnsureNoErrors();
    }

    /// <summary>
    /// Asserts that the connector has not raised any errors so far.
    /// </summary>
    protected void EnsureNoErrors()
    {
        _errors.IsEmpty();
    }

    /// <summary>
    /// Records the current balance and position so a later assertion (e.g. <see cref="EnsureBalanceIsIncreased"/>)
    /// can compare against them.
    /// </summary>
    protected void Snapshot()
    {
        this.Trace("start");

        _balance = GetBalance(Instrument.Currency.Code);
        _position = GetPosition();

        this.Trace("done");
    }

    /// <summary>
    /// Market-closes every active position reported so far by placing an opposite-side order for its amount
    /// on the real account. A no-op if there are no active positions.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task CloseActivePositions()
    {
        this.Trace("start");

        // a short position is a negative amount, and filtering for `> 0` skipped every one of them -
        // which also left the `Amount < 0` branch below unreachable. A short left open here is a real
        // open position on a real account, carried into whatever test runs next
        var activePositions = _positions
            .DistinctBy(x => new { x.Symbol, x.OrientationRange })
            .Where(x => x.Amount != 0)
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

    /// <summary>
    /// Sends an order-init request expected to be rejected by the real exchange, and asserts that it fails
    /// without placing an order.
    /// </summary>
    /// <param name="request">The order-init request expected to be rejected.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected async Task InitInvalidOrder(IInitOrderRequest request)
    {
        this.Trace("start");

        await Connector.InitOrderAsync(request).EnsureFailedAsync();

        EnsureNoErrors();

        this.Trace("done");
    }

    /// <summary>
    /// Places a real order on the account via the given order-init request, and asserts it comes back
    /// matching the request and is reported with the expected status.
    /// </summary>
    /// <param name="request">The order-init request to place.</param>
    /// <param name="status">The status the order is expected to be reported with.</param>
    /// <returns>The placed order as reported by the connector.</returns>
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

    /// <summary>
    /// Sends a cancel request for the given real order expected to be rejected by the exchange, and asserts
    /// that it fails without canceling the order.
    /// </summary>
    /// <param name="order">The order the cancellation is expected to be rejected for.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected async Task CancelInvalidOrder(OrderModel order)
    {
        this.Trace("start");

        var request = CancelOrder(order.Id, order.ClientOrderId, order.Symbol);
        await Connector.CancelOrderAsync(request).EnsureFailedAsync();

        EnsureNoErrors();

        this.Trace("done");
    }

    /// <summary>
    /// Cancels the given real order on the account, and asserts it comes back reported as canceled.
    /// </summary>
    /// <param name="order">The order to cancel.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
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

    /// <summary>
    /// Sends a modify request expected to be rejected by the real exchange, and asserts that it fails
    /// without modifying the order.
    /// </summary>
    /// <param name="request">The modify request expected to be rejected.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected async Task ModifyInvalidOrder(IModifyOrderRequest request)
    {
        this.Trace("start");

        await Connector.ModifyOrderAsync(request).EnsureFailedAsync();

        EnsureNoErrors();

        this.Trace("done");
    }

    /// <summary>
    /// Modifies a real order on the account via the given modify request, and asserts it comes back matching
    /// the request and is reported with the expected status.
    /// </summary>
    /// <param name="request">The modify request to send.</param>
    /// <param name="status">The status the order is expected to be reported with.</param>
    /// <returns>The modified order as reported by the connector.</returns>
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

    /// <summary>
    /// Cancels every open order on <see cref="Symbol"/> on the real account.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected async Task CancelOpenOrders()
    {
        this.Trace("cancel all orders - start");

        // cancel existing orders
        await Connector.CancelAllOrdersAsync(Symbol).UnwrapAsync();

        EnsureNoErrors();

        this.Trace("cancel all orders - done");
    }

    /// <summary>
    /// Waits until the connector has reported at least one asset balance, plus a fixed grace period for
    /// further messages to settle.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected async Task AwaitForInitialBalances()
    {
        // await until balances arrive and a second more before starting test
        this.Trace("await for balances");
        await Expect.ToAsync(() => _assets.IsNotEmpty());
        await WaitForMessages();
    }

    /// <summary>
    /// Waits until the connector has reported at least one position update.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected ValueTask AwaitForInitialPositionsAndLeverages()
    {
        this.Trace("await for positions");
        return Expect.ToAsync(() => _positions.IsNotEmpty());
    }

    /// <summary>
    /// Gets the most recently reported position amount on <see cref="Symbol"/>, rounded to the instrument's lot size.
    /// </summary>
    /// <returns>The position amount, rounded to the instrument's lot size.</returns>
    protected decimal GetPositionAmount()
    {
        this.Trace<string>("get size of {symbol} position", Symbol);
        var amount = GetPosition().Amount;

        return Instrument.ToLotSize(amount);
    }

    /// <summary>Generates a fresh, random client order id for a new request.</summary>
    /// <returns>A new random client order id.</returns>
    protected string ClientOrderId() => Guid.NewGuid().ToString();

    /// <summary>Gets the orientation range used for requests in this scenario.</summary>
    /// <returns>Always <see cref="OrientationRange.Both"/>.</returns>
    protected OrientationRange Range() => OrientationRange.Both;

    /// <summary>
    /// Gets the most recently reported balance for the given asset.
    /// </summary>
    /// <param name="resource">The asset code to get the balance of.</param>
    /// <returns>The most recently reported balance for the asset.</returns>
    protected AssetModel GetBalance(string resource)
    {
        this.Trace<string>("get {resource} last balance", resource);
        var asset = _assets.Last(x => x.Resource == resource);
        this.Trace("got {resource} last balance: {asset}", resource, asset);

        return asset;
    }

    /// <summary>
    /// Gets the most recently reported both-orientation position on <see cref="Symbol"/>.
    /// </summary>
    /// <returns>The most recently reported position on <see cref="Symbol"/>.</returns>
    private PositionModel GetPosition()
    {
        this.Trace("get {instrument} position", Instrument);
        var position = _positions.Last(x => x.OrientationRange is OrientationRange.Both && x.Symbol == Symbol);
        this.Trace("got {instrument} last position: {position}", Instrument, position);

        return position;
    }

    /// <summary>
    /// Asserts that the current currency balance has more locked and less free than at the last <see cref="Snapshot"/>.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected ValueTask EnsureBalanceIsLocked()
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

    /// <summary>
    /// Asserts that the current currency balance has more free and less locked than at the last <see cref="Snapshot"/>.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected ValueTask EnsureBalanceIsReleased()
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

    /// <summary>
    /// Asserts that the current currency free balance is greater than at the last <see cref="Snapshot"/>.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected ValueTask EnsureBalanceIsIncreased()
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

    /// <summary>
    /// Asserts that the current currency free balance is smaller than at the last <see cref="Snapshot"/>.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected ValueTask EnsureBalanceIsDecreased()
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

    /// <summary>
    /// Asserts that the current position amount on <see cref="Symbol"/> is greater than at the last <see cref="Snapshot"/>.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected ValueTask EnsurePositionIsIncreased()
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

    /// <summary>
    /// Asserts that the current position amount on <see cref="Symbol"/> is smaller than at the last <see cref="Snapshot"/>.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected ValueTask EnsurePositionIsDecreased()
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

    /// <summary>
    /// Waits until the connector reports an update for the given order matching it and carrying the expected status.
    /// </summary>
    /// <param name="order">The order expected to be reported.</param>
    /// <param name="status">The status the order is expected to be reported with.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private ValueTask EnsureOrderReported(OrderModel order, OrderStatus status)
    {
        this.Trace("ensure order {order} is reported and has status {status}", order.Id, status);
        return Expect.ToAsync(() =>
        {
            var orderMessage = _orders.Last(x => x.Id == order.Id);
            orderMessage.ShouldMatch(order);
            orderMessage.Status.Is(status);
        });
    }

    /// <summary>
    /// Pauses for a fixed grace period to let in-flight connector messages settle.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    private Task WaitForMessages()
    {
        this.Trace("await for messages");
        return Task.Delay(1000);
    }
}
