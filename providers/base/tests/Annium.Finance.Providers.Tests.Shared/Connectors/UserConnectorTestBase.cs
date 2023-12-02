using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Annium.Core.Mapper;
using Annium.Extensions.Pooling;
using Annium.Finance.Providers.Abstractions.Connectors.Connectors;
using Annium.Finance.Providers.Abstractions.Domain.Dto;
using Annium.Finance.Providers.Abstractions.Domain.Enums;
using Annium.Finance.Providers.Abstractions.Domain.Interfaces;
using Annium.Finance.Providers.Shared;
using Annium.Finance.Providers.Tests.Shared.Extensions;
using Annium.Logging;
using Annium.Testing;
using Xunit.Abstractions;

namespace Annium.Finance.Providers.Tests.Shared.Connectors;

public abstract class UserConnectorTestBase : ConnectorTestBase
{
    protected InstrumentDto Instrument { get; private set; } = default!;
    protected IUserConnector Connector { get; private set; } = default!;
    protected AsyncDisposableBox Disposable { get; set; }
    private readonly IUserConfig _config;
    private readonly string _symbol;
    private readonly ConcurrentQueue<ConnectorError> _errors = new();
    private AssetDto _balance = default!;

    protected UserConnectorTestBase(
        Action<ProviderRegistrationContext> registerProvider,
        IUserConfig config,
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

        var marketConfig = Get<IMapper>().Map<IMarketConfig>(_config);
        var marketConnectorRef = await Get<IObjectCache<IMarketConfig, IMarketConnector>>().GetAsync(marketConfig);
        Disposable += marketConnectorRef;

        Instrument = marketConnectorRef.Value.Instruments.Single(x => x.Symbol == _symbol);

        var userConnectorRef = await Get<IObjectCache<IUserConfig, IUserConnector>>().GetAsync(_config);
        Disposable += userConnectorRef;
        Connector = userConnectorRef.Value;

        Connector.OnError += _errors.Enqueue;

        this.Trace("done");
    }

    protected async Task DisposeBaseAsync()
    {
        this.Trace("start");

        this.Trace("dispose disposables");
        await Disposable.DisposeAsync();

        this.Trace("done");
    }

    protected void EnsureNoErrors()
    {
        _errors.IsEmpty();
    }

    protected virtual void Snapshot()
    {
        this.Trace("start");

        _balance = GetBalance(Instrument.Currency.Code);

        this.Trace("done");
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
        await Expect.To(() => Connector.Assets.IsNotEmpty());
        await WaitForMessages();
    }

    protected Guid GenerateClientOrderId() => Guid.NewGuid();

    protected AssetDto GetBalance(string resource)
    {
        this.Trace<string>("get {resource} last balance of {0}", resource);
        return Connector.Assets.Single(x => x.Resource == resource);
    }

    protected async Task EnsureBalanceIsLocked()
    {
        var originalBalance = _balance;

        this.Trace<string>(
            "ensure current balance is locked compared to original {balance}",
            JsonSerializer.Serialize(originalBalance)
        );

        await Expect.To(() =>
        {
            var currentBalance = GetBalance(Instrument.Currency.Code);
            currentBalance.Free.IsLess(originalBalance.Free);
            currentBalance.Locked.IsGreater(originalBalance.Locked);
        });
    }

    protected async Task EnsureBalanceIsReleased()
    {
        var originalBalance = _balance;

        this.Trace<string>(
            "ensure current balance is released compared to original {balance}",
            JsonSerializer.Serialize(originalBalance)
        );

        await Expect.To(() =>
        {
            var currentBalance = GetBalance(Instrument.Currency.Code);
            currentBalance.Free.IsGreater(originalBalance.Free);
            currentBalance.Locked.IsLess(originalBalance.Locked);
        });
    }

    protected async Task EnsureBalanceIsIncreased()
    {
        var originalBalance = _balance;

        this.Trace<string>(
            "ensure current free balance is greater than original {balance}",
            JsonSerializer.Serialize(originalBalance)
        );

        await Expect.To(() =>
        {
            var currentBalance = GetBalance(Instrument.Currency.Code);
            currentBalance.Free.IsGreater(originalBalance.Free);
        });
    }

    protected async Task EnsureBalanceIsDecreased()
    {
        var originalBalance = _balance;

        this.Trace<string>(
            "ensure current free balance is smaller than original {balance}",
            JsonSerializer.Serialize(originalBalance)
        );

        await Expect.To(() =>
        {
            var currentBalance = GetBalance(Instrument.Currency.Code);
            currentBalance.Free.IsLess(originalBalance.Free);
        });
    }

    private async Task EnsureOrderReported(OrderDto order, OrderStatus status)
    {
        this.Trace("ensure order {order} is reported and has status {status}", order.OrderId, status);
        await Expect.To(() =>
        {
            var orderMessage = Connector.Orders.Single(x => x.OrderId == order.OrderId);
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
