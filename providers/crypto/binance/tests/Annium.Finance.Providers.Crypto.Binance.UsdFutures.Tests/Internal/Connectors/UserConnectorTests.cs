using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Domain.Market;
using Annium.Finance.Providers.Abstractions.Domain.User;
using Annium.Finance.Providers.Abstractions.Domain.User.Requests;
using Annium.Finance.Providers.Shared;
using Annium.Finance.Providers.Shared.Loaders;
using Annium.Finance.Providers.Tests.Lib.Connectors;
using Annium.Logging;
using Xunit;
using static Annium.Finance.Providers.Abstractions.Domain.User.Requests.RequestBuilder;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Tests.Internal.Connectors;

public class UserConnectorTests : UserConnectorTestBase
{
    private decimal ExtremeHighQty => Instrument.MaxQty * 1_000_000;
    private decimal ExtremeHighPrice => Instrument.ToTickSizeDown(Ticker.Price() * 1_000_000m);
    private decimal HighPrice => Instrument.ToTickSizeDown(Ticker.Price() * 1.3m);
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
        : base(Settings.User, "DOTUSDT", output) { }

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

    [Fact]
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

    [Fact]
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

    [Fact]
    public async Task InitOrder_Market_Invalid()
    {
        this.Trace("start");

        var request = InitMarketOrder(ClientOrderId(), Range(), Symbol, OrderSide.Buy, ExtremeHighQty);
        await InitInvalidOrder(request);

        this.Trace("done");
    }

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
    public async Task CancelAllOrders()
    {
        this.Trace("start");

        await CancelOpenOrders();

        this.Trace("done");
    }
}
