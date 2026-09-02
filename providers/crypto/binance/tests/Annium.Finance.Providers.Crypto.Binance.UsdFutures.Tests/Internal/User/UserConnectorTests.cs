using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Domain.Market;
using Annium.Finance.Providers.Abstractions.Domain.User;
using Annium.Finance.Providers.Abstractions.Domain.User.Requests;
using Annium.Finance.Providers.Core;
using Annium.Finance.Providers.Core.Shared.Loaders;
using Annium.Finance.Providers.Tests.Lib;
using Annium.Finance.Providers.Tests.Lib.User;
using Annium.Logging;
using Xunit;
using static Annium.Finance.Providers.Abstractions.Domain.User.Requests.RequestBuilder;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Tests.Internal.User;

/// <summary>
/// Runs <see cref="UserConnectorTestBase"/>'s scenarios against the real Binance USD-M futures user
/// connector for DOTUSDT, under the account in <see cref="Settings.User"/>. These tests place, fill, modify
/// and cancel real orders on that account - the base class actively manages its state around each test,
/// canceling open orders and flattening any position on setup and teardown - so they run only when
/// the write block is asked for, and must never point at an account you care about.
/// </summary>
[Collection(ExchangeCollection.Name)]
public class UserConnectorTests : UserConnectorTestBase
{
    /// <summary>A quantity far past the instrument's max, used to force an order rejection.</summary>
    private decimal ExtremeHighQty => Instrument.MaxQty * 1_000_000;

    /// <summary>A price far above the current ticker, used to force a trigger-price rejection.</summary>
    private decimal ExtremeHighPrice => Instrument.ToTickSizeDown(Ticker.Price() * 1_000_000m);

    /// <summary>A price moderately above the current ticker, valid as a take-profit/stop trigger.</summary>
    private decimal HighPrice => Instrument.ToTickSizeDown(Ticker.Price() * 1.3m);

    /// <summary>A price moderately below the current ticker, valid as a stop-loss/limit-buy trigger.</summary>
    private decimal LowPrice => Instrument.ToTickSizeDown(Ticker.Price() * 0.7m);

    /// <summary>The smallest quantity that still clears the instrument's minimum notional at <see cref="LowPrice"/>.</summary>
    private decimal MinQty
    {
        get
        {
            var minQty = Instrument.ToValidQty(Instrument.MinSum / LowPrice);
            return minQty + (minQty * LowPrice > Instrument.MinSum ? 0 : Instrument.LotSize);
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UserConnectorTests"/> class, targeting DOTUSDT under the
    /// configured <see cref="Settings.User"/> account.
    /// </summary>
    /// <param name="output">The xUnit output helper to route trace logging to.</param>
    public UserConnectorTests(ITestOutputHelper output)
        : base(Settings.User, "DOTUSDT", output) { }

    /// <summary>
    /// Registers the Binance USD-M futures provider, with tight reload-loader intervals, so the connector
    /// under test is resolved from its actual registration.
    /// </summary>
    /// <param name="ctx">The fluent context to register providers into.</param>
    protected override void RegisterProvider(ProviderRegistrationContext ctx)
    {
        ctx.WithBinanceUsdFutures(
            new ProviderConfiguration
            {
                ReloadContext = new CompositeLoaderConfig(200, 5, 1000, 1000, 100),
                ReloadOrders = new CompositeLoaderConfig(200, 5, 1000, 1000, 100),
                ReloadTrades = new CompositeLoaderConfig(200, 5, 1000, 1000, 100),
            }
        );
    }

    /// <summary>
    /// Sends a limit buy with an absurd quantity and asserts the real exchange rejects it. Talks to the
    /// real exchange and mutates the account; in the **write** block.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact(
        Skip = "needs exchange credentials in test.env",
        SkipUnless = nameof(Exchange.HasCredentials),
        SkipType = typeof(Exchange)
    )]
    public async Task InitOrder_Limit_Invalid()
    {
        this.Trace("start");

        var request = InitLimitOrder(
            ClientOrderId(),
            OrientationRange.Both,
            Symbol,
            OrderSide.Buy,
            ExtremeHighQty,
            LowPrice
        );
        await InitInvalidOrder(request);

        this.Trace("done");
    }

    /// <summary>
    /// Places a real minimum-size limit buy, asserts the currency balance locks up around it, then cancels
    /// it and asserts the balance is released. Talks to the real exchange and places/cancels a real order;
    /// in the **write** block.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact(
        Skip = "needs exchange credentials in test.env",
        SkipUnless = nameof(Exchange.HasCredentials),
        SkipType = typeof(Exchange)
    )]
    public async Task InitOrder_Limit_Valid()
    {
        this.Trace("start");

        // arrange
        var request = InitLimitOrder(ClientOrderId(), Range(), Symbol, OrderSide.Buy, MinQty, LowPrice);

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

    /// <summary>
    /// Sends a market buy with an absurd quantity and asserts the real exchange rejects it. Talks to the
    /// real exchange and mutates the account; in the **write** block.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact(
        Skip = "needs exchange credentials in test.env",
        SkipUnless = nameof(Exchange.HasCredentials),
        SkipType = typeof(Exchange)
    )]
    public async Task InitOrder_Market_Invalid()
    {
        this.Trace("start");

        var request = InitMarketOrder(ClientOrderId(), Range(), Symbol, OrderSide.Buy, ExtremeHighQty);
        await InitInvalidOrder(request);

        this.Trace("done");
    }

    /// <summary>
    /// Opens a real position with a market buy, then exercises all four trigger order types (stop-loss and
    /// take-profit, each market and limit) via <see cref="TestOrder"/> - each rejected first with an
    /// absurd trigger, then placed for real and canceled - before closing the position with a market sell
    /// and asserting balance/position moved as expected throughout. Talks to the real exchange and
    /// places/cancels/fills several real orders; in the **write** block.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact(
        Skip = "needs exchange credentials in test.env",
        SkipUnless = nameof(Exchange.HasCredentials),
        SkipType = typeof(Exchange)
    )]
    public async Task InitOrder_TakeProfit_StopLoss()
    {
        this.Trace("start");

        // arrange
        var request = InitMarketOrder(ClientOrderId(), Range(), Symbol, OrderSide.Buy, MinQty);

        // open position
        var order = await InitValidOrder(request, OrderStatus.Filled);
        await EnsureBalanceIsDecreased();
        await EnsurePositionIsIncreased();

        // try cleanup
        await CancelInvalidOrder(order);

        // TP & SL invalid orders
        await TestOrder(
            InitStopLossMarketOrder(ClientOrderId(), Range(), Symbol, OrderSide.Sell, ExtremeHighQty, LowPrice),
            InitStopLossMarketOrder(ClientOrderId(), Range(), Symbol, OrderSide.Sell, GetPositionAmount(), LowPrice)
        );

        await TestOrder(
            InitTakeProfitMarketOrder(
                ClientOrderId(),
                Range(),
                Symbol,
                OrderSide.Sell,
                GetPositionAmount(),
                ExtremeHighPrice
            ),
            InitTakeProfitMarketOrder(ClientOrderId(), Range(), Symbol, OrderSide.Sell, GetPositionAmount(), HighPrice)
        );

        await TestOrder(
            InitStopLossLimitOrder(
                ClientOrderId(),
                Range(),
                Symbol,
                OrderSide.Sell,
                ExtremeHighQty,
                LowPrice + Instrument.TickSize,
                LowPrice
            ),
            InitStopLossLimitOrder(
                ClientOrderId(),
                Range(),
                Symbol,
                OrderSide.Sell,
                GetPositionAmount(),
                LowPrice + Instrument.TickSize,
                LowPrice
            )
        );

        await TestOrder(
            InitTakeProfitLimitOrder(
                ClientOrderId(),
                Range(),
                Symbol,
                OrderSide.Sell,
                GetPositionAmount(),
                HighPrice + Instrument.TickSize,
                ExtremeHighPrice
            ),
            InitTakeProfitLimitOrder(
                ClientOrderId(),
                Range(),
                Symbol,
                OrderSide.Sell,
                GetPositionAmount(),
                HighPrice + Instrument.TickSize,
                HighPrice
            )
        );

        // cleanup
        request = InitMarketOrder(ClientOrderId(), Range(), Symbol, OrderSide.Sell, GetPositionAmount());
        await InitValidOrder(request, OrderStatus.Filled);
        await EnsureBalanceIsIncreased();
        await EnsurePositionIsDecreased();

        this.Trace("done");
    }

    /// <summary>
    /// Sends <paramref name="invalidRequest"/> and asserts the real exchange rejects it, then places
    /// <paramref name="validRequest"/> for real and cancels it. Talks to the real exchange and
    /// places/cancels a real order.
    /// </summary>
    /// <param name="invalidRequest">A request expected to be rejected by the exchange.</param>
    /// <param name="validRequest">A request expected to be accepted, which is placed and then canceled.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task TestOrder(IInitOrderRequest invalidRequest, IInitOrderRequest validRequest)
    {
        this.Trace("start {0} order tet", invalidRequest.Type);

        this.Trace("init invalid {0} order", invalidRequest.Type);
        await InitInvalidOrder(invalidRequest);

        this.Trace("init valid {0} order", validRequest.Type);
        var order = await InitValidOrder(validRequest, OrderStatus.New);

        this.Trace("cancel valid {0} order", validRequest.Type);
        await CancelValidOrder(order);
    }

    /// <summary>
    /// Places a real limit order, then sends a modify request with an absurd quantity and asserts the real
    /// exchange rejects it. Talks to the real exchange and places a real order; skipped unless
    /// the **write** block is asked for.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact(
        Skip = "needs exchange credentials in test.env",
        SkipUnless = nameof(Exchange.HasCredentials),
        SkipType = typeof(Exchange)
    )]
    public async Task ModifyOrder_Invalid()
    {
        this.Trace("start");

        // arrange
        this.Trace("init order");
        var initRequest = InitLimitOrder(ClientOrderId(), Range(), Symbol, OrderSide.Buy, MinQty, LowPrice);
        var initOrder = await InitValidOrder(initRequest, OrderStatus.New);
        var modifyRequest = ModifyToLimitOrder(initOrder, initOrder.Side, ExtremeHighQty, initOrder.Price);

        // act
        this.Trace("modify invalid order");
        await ModifyInvalidOrder(modifyRequest);

        this.Trace("done");
    }

    /// <summary>
    /// Places a real limit order, modifies it to a larger quantity and higher price, asserts the currency
    /// balance locks up around the modified order, then cancels it and asserts the balance is released.
    /// Talks to the real exchange and places/modifies/cancels a real order; skipped unless
    /// the **write** block is asked for.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact(
        Skip = "needs exchange credentials in test.env",
        SkipUnless = nameof(Exchange.HasCredentials),
        SkipType = typeof(Exchange)
    )]
    public async Task ModifyOrder_Valid()
    {
        this.Trace("start");

        // arrange
        this.Trace("init order");
        var initRequest = InitLimitOrder(ClientOrderId(), Range(), Symbol, OrderSide.Buy, MinQty, LowPrice);
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

    /// <summary>
    /// Places a real limit order, cancels it and asserts the currency balance locks then releases around
    /// the cancellation, then asserts canceling the same (now-canceled) order again is rejected. Talks to
    /// the real exchange and places/cancels a real order; only ever selected by the write block
    /// is set.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact(
        Skip = "needs exchange credentials in test.env",
        SkipUnless = nameof(Exchange.HasCredentials),
        SkipType = typeof(Exchange)
    )]
    public async Task CancelOrder()
    {
        this.Trace("start");

        // arrange
        var request = InitLimitOrder(ClientOrderId(), Range(), Symbol, OrderSide.Buy, MinQty, LowPrice);

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

    /// <summary>
    /// Cancels every open order on the account for <see cref="UserConnectorTestBase.Symbol"/>. Talks to the
    /// real exchange and mutates the account; in the **write** block.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact(
        Skip = "needs exchange credentials in test.env",
        SkipUnless = nameof(Exchange.HasCredentials),
        SkipType = typeof(Exchange)
    )]
    public async Task CancelAllOrders()
    {
        this.Trace("start");

        await CancelOpenOrders();

        this.Trace("done");
    }
}
