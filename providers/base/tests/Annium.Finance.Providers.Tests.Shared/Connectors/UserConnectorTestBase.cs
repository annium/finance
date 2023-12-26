using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reactive.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Annium.Core.Mapper;
using Annium.Extensions.Pooling;
using Annium.Finance.Providers.Abstractions.Connectors.Connectors;
using Annium.Finance.Providers.Abstractions.Domain.Dto;
using Annium.Finance.Providers.Abstractions.Domain.Enums;
using Annium.Finance.Providers.Abstractions.Domain.Extensions;
using Annium.Finance.Providers.Abstractions.Domain.Interfaces;
using Annium.Finance.Providers.Abstractions.Domain.Models;
using Annium.Finance.Providers.Shared;
using Annium.Finance.Providers.Tests.Shared.Extensions;
using Annium.Logging;
using Annium.Testing;
using Xunit.Abstractions;
using static Annium.Finance.Providers.Abstractions.Domain.Tools.RequestBuilder;

namespace Annium.Finance.Providers.Tests.Shared.Connectors;

public abstract class UserConnectorTestBase : ConnectorTestBase
{
    protected InstrumentDto Instrument { get; private set; } = default!;
    protected InstrumentTicker Ticker { get; private set; } = default!;
    private IUserConnector Connector { get; set; } = default!;
    private AsyncDisposableBox Disposable { get; set; }
    private readonly UserSettings _config;
    private readonly string _symbol;
    private readonly ConcurrentQueue<AssetDto> _assets = new();
    private readonly ConcurrentQueue<PositionDto> _positions = new();
    private readonly ConcurrentQueue<OrderDto> _orders = new();
    private readonly ConcurrentQueue<TradeDto> _trades = new();
    private readonly ConcurrentQueue<ConnectorError> _errors = new();
    private AssetDto _balance = default!;
    private PositionDto _position = default!;

    protected UserConnectorTestBase(
        Action<ProviderRegistrationContext> registerProvider,
        UserSettings config,
        string symbol,
        ITestOutputHelper output
    )
        : base(registerProvider, output)
    {
        Disposable = Annium.Disposable.AsyncBox(Logger);
        _config = config;
        _symbol = symbol;
    }

    protected async Task InitializeBaseAsync()
    {
        this.Trace("start");

        var marketConfig = Get<IMapper>().Map<MarketSettings>(_config);
        this.Trace("get market connector for {config}", marketConfig);
        var marketConnectorRef = await Get<IObjectCache<MarketSettings, IMarketConnector>>().GetAsync(marketConfig);
        Disposable += marketConnectorRef;
        var market = marketConnectorRef.Value;

        this.Trace("await until market connector is ready");
        await market.WhenConnected();

        this.Trace<string>("find instrument {symbol}", _symbol);
        Instrument = market.Instruments.Single(x => x.Symbol == _symbol);
        this.Trace("found instrument {instrument}", Instrument);

        this.Trace<string>("subscribe and wait for ticker for {symbol}", _symbol);
        market.SubscribeTickers(new[] { Instrument.Symbol });
        this.Trace<string>("find ticker for {symbol}", _symbol);
        Ticker = await market.Tickers.FirstAsync(x => x.Symbol == Instrument.Symbol);
        this.Trace("found ticker for {instrument}", Instrument);

        this.Trace("get user connector for {config}", _config);
        var userConnectorRef = await Get<IObjectCache<UserSettings, IUserConnector>>().GetAsync(_config);
        Disposable += userConnectorRef;
        Connector = userConnectorRef.Value;

        this.Trace("subscribe to connector data");
        Disposable += Connector.Assets.Subscribe(_assets.Enqueue);
        Disposable += Connector.Positions.Subscribe(_positions.Enqueue);
        Disposable += Connector.Orders.Subscribe(_orders.Enqueue);
        Disposable += Connector.Trades.Subscribe(_trades.Enqueue);

        this.Trace("subscribe to connector errors");
        Connector.OnError += _errors.Enqueue;

        this.Trace("await until user connector is ready");
        await Connector.WhenConnected();

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

    protected async Task DisposeBaseAsync()
    {
        this.Trace("start");

        this.Trace("cancel open orders");
        await CancelOpenOrders();

        this.Trace("try close position if any");
        var amount = GetPositionAmount();
        if (amount > 0)
        {
            this.Trace("close position amount: {0}", amount);
            await InitValidOrder(
                InitMarketOrder(GenerateClientOrderId(), Instrument.Symbol, OrderSide.Sell, amount),
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
            this.Trace("close {0} position with amount {1}", Instrument, position.Amount);
            await Connector
                .InitOrder(
                    InitMarketOrder(
                        GenerateClientOrderId(),
                        Instrument.Symbol,
                        position.Amount < 0 ? OrderSide.Buy : OrderSide.Sell,
                        Math.Abs(position.Amount)
                    )
                )
                .Unwrap();
        }

        EnsureNoErrors();

        this.Trace("close active positions - done");
    }

    protected async Task InitInvalidOrder(IInitOrderRequest request)
    {
        this.Trace("start");

        await Connector.InitOrder(request).EnsureFailed();

        EnsureNoErrors();

        this.Trace("done");
    }

    protected async Task<OrderDto> InitValidOrder(IInitOrderRequest request, OrderStatus status)
    {
        this.Trace("start");

        Snapshot();

        // act
        this.Trace("execute start");
        var order = await Connector.InitOrder(request).Unwrap();
        this.Trace("execute done");

        EnsureNoErrors();

        // assert
        order.ShouldMatch(request);
        await EnsureOrderReported(order, status);

        EnsureNoErrors();

        this.Trace("done");

        return order;
    }

    protected async Task CancelInvalidOrder(OrderDto order)
    {
        this.Trace("start");

        await Connector.CancelOrder(order).EnsureFailed();

        EnsureNoErrors();

        this.Trace("done");
    }

    protected async Task CancelValidOrder(OrderDto order)
    {
        this.Trace("start");

        Snapshot();

        // cleanup
        this.Trace("execute start");
        await Connector.CancelOrder(order).Unwrap();
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

        await Connector.ModifyOrder(request).EnsureFailed();

        EnsureNoErrors();

        this.Trace("done");
    }

    protected async Task<OrderDto> ModifyValidOrder(IModifyOrderRequest request, OrderStatus status)
    {
        this.Trace("start");

        Snapshot();

        // act
        this.Trace("execute start");
        var order = await Connector.ModifyOrder(request).Unwrap();
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
        await Connector.CancelAllOrders(Instrument.Symbol).Unwrap();

        EnsureNoErrors();

        this.Trace("cancel all orders - done");
    }

    protected async Task AwaitForInitialBalances()
    {
        // await until balances arrive and a second more before starting test
        this.Trace("await for balances");
        await Expect.To(() => _assets.IsNotEmpty());
        await WaitForMessages();
    }

    protected Task AwaitForInitialPositionsAndLeverages()
    {
        this.Trace("await for positions");
        return Expect.To(() => _positions.IsNotEmpty());
    }

    protected decimal GetPositionAmount()
    {
        this.Trace<string>("get size of {symbol} position", Instrument.Symbol);
        var amount = GetPosition().Amount;

        return Instrument.ToLotSize(amount);
    }

    protected string GenerateClientOrderId() => Guid.NewGuid().ToString();

    protected AssetDto GetBalance(string resource)
    {
        this.Trace<string>("get {resource} last balance", resource);
        var asset = _assets.Last(x => x.Resource == resource);
        this.Trace("got {resource} last balance: {asset}", resource, asset);

        return asset;
    }

    private PositionDto GetPosition()
    {
        this.Trace<string>("get {0} position", Instrument.Symbol);
        return _positions.Last(x => x.OrientationRange is OrientationRange.Both && x.Symbol == Instrument.Symbol);
    }

    protected Task EnsureBalanceIsLocked()
    {
        var originalBalance = _balance;

        this.Trace<string>(
            "ensure current balance is locked compared to original {balance}",
            JsonSerializer.Serialize(originalBalance)
        );

        return Expect.To(() =>
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

        return Expect.To(() =>
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

        return Expect.To(() =>
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

        return Expect.To(() =>
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

        return Expect.To(() =>
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

        return Expect.To(() =>
        {
            var currentPosition = GetPosition();
            currentPosition.Amount.IsLess(originalPosition.Amount);
        });
    }

    private Task EnsureOrderReported(OrderDto order, OrderStatus status)
    {
        this.Trace("ensure order {order} is reported and has status {status}", order.Id, status);
        return Expect.To(() =>
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
