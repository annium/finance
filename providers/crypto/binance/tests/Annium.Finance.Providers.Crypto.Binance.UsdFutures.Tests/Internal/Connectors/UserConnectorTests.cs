using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Domain.Enums;
using Annium.Finance.Providers.Abstractions.Domain.Extensions;
using Annium.Finance.Providers.Abstractions.Domain.Models;
using Annium.Finance.Providers.Shared.Services;
using Annium.Finance.Providers.Tests.Shared.Connectors;
using Annium.Logging;
using Xunit;
using Xunit.Abstractions;
using static Annium.Finance.Providers.Abstractions.Domain.Tools.RequestBuilder;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Tests.Internal.Connectors;

public class UserConnectorTests : UserConnectorTestBase
{
    private decimal ExtremeHighQty => Instrument.MaxQty * 1_000_000;
    private decimal LowPrice => Instrument.ToTickSizeDown(Ticker.Price() * 0.7m);

    private decimal MinQty
    {
        get
        {
            var minQty = Instrument.ToValidQty(Instrument.MinSum / LowPrice);
            return minQty + (minQty * LowPrice > Instrument.MinSum ? 0 : Instrument.LotSize);
        }
    }

    public UserConnectorTests(ITestOutputHelper output)
        : base(
            ctx =>
                ctx.WithBinanceUsdFutures(
                    new ProviderConfiguration
                    {
                        ReloadAccount = new CompositeLoaderConfig(200, 5, 1000, 1000, 100),
                        ReloadOrders = new CompositeLoaderConfig(200, 5, 1000, 1000, 100),
                        ReloadTrades = new CompositeLoaderConfig(200, 5, 1000, 1000, 100),
                    }
                ),
            new UserSettings(
                Constants.Provider,
                ProviderEnvironment.Test,
                "19136244bcbe0adb854f5234451ddf80c440ca7372fde16cb06178900712e8ba",
                "493495031de246dd8cfbcb3a3676df563c99abaf1240105af34567d440c1406e"
            ),
            "BTCUSDT",
            output
        ) { }

    [Fact]
    public async Task InitOrder_Invalid()
    {
        this.Trace("start");

        var request = InitLimitOrder(
            GenerateClientOrderId(),
            Instrument.Symbol,
            OrderSide.Buy,
            ExtremeHighQty,
            LowPrice
        );
        await InitInvalidOrder(request);

        this.Trace("done");
    }

    [Fact]
    public async Task InitOrder_Valid()
    {
        this.Trace("start");

        // arrange
        var request = InitLimitOrder(GenerateClientOrderId(), Instrument.Symbol, OrderSide.Buy, MinQty, LowPrice);

        // act
        this.Trace("init order");
        var order = await InitValidOrder(request, OrderStatus.New);

        this.Trace("ensure balance is locked");
        await EnsureBalanceIsLocked();

        // cleanup
        this.Trace("cancel order");
        await CancelValidOrder(order);

        this.Trace("ensure balance is released");
        await EnsureBalanceIsReleased();

        this.Trace("done");
    }

    [Fact]
    public async Task ModifyOrder_Invalid()
    {
        this.Trace("start");

        // arrange
        this.Trace("init order");
        var initRequest = InitLimitOrder(GenerateClientOrderId(), Instrument.Symbol, OrderSide.Buy, MinQty, LowPrice);
        var initOrder = await InitValidOrder(initRequest, OrderStatus.New);
        var modifyRequest = ModifyToLimitOrder(initOrder, initOrder.Side, ExtremeHighQty, initOrder.Price);

        // act
        this.Trace("modify invalid order");
        await ModifyInvalidOrder(modifyRequest);

        this.Trace("done");
    }

    [Fact]
    public async Task ModifyOrder_Valid()
    {
        this.Trace("start");

        // arrange
        this.Trace("init order");
        var initRequest = InitLimitOrder(GenerateClientOrderId(), Instrument.Symbol, OrderSide.Buy, MinQty, LowPrice);
        var initialOrder = await InitValidOrder(initRequest, OrderStatus.New);
        var modifyRequest = ModifyToLimitOrder(
            initialOrder,
            initialOrder.Side,
            initialOrder.TotalQty + Instrument.LotSize,
            initialOrder.Price + Instrument.TickSize
        );

        // act
        this.Trace("modify invalid order");
        var modifiedOrder = await ModifyValidOrder(modifyRequest, OrderStatus.New);

        this.Trace("ensure balance is locked");
        await EnsureBalanceIsLocked();

        // cleanup
        this.Trace("cancel valid order");
        await CancelValidOrder(modifiedOrder);

        this.Trace("ensure balance is released");
        await EnsureBalanceIsReleased();

        this.Trace("done");
    }

    [Fact]
    public async Task CancelOrder()
    {
        this.Trace("start");

        // arrange
        var request = InitLimitOrder(GenerateClientOrderId(), Instrument.Symbol, OrderSide.Buy, MinQty, LowPrice);

        // act
        this.Trace("init order");
        var order = await InitValidOrder(request, OrderStatus.New);

        this.Trace("ensure balance is locked");
        await EnsureBalanceIsLocked();

        // cleanup
        this.Trace("cancel valid order");
        await CancelValidOrder(order);

        this.Trace("ensure balance is released");
        await EnsureBalanceIsReleased();

        // assert
        this.Trace("cancel invalid order");
        await CancelInvalidOrder(order);

        this.Trace("done");
    }

    [Fact]
    public async Task CancelAllOrders()
    {
        this.Trace("start");

        await CancelOpenOrders();

        this.Trace("done");
    }
}
