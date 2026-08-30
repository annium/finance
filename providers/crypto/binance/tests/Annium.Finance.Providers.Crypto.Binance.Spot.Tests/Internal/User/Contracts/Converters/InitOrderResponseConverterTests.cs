using System.Text;
using Annium.Finance.Providers.Abstractions.Domain.User;
using Annium.Finance.Providers.Core;
using Annium.Finance.Providers.Tests.Lib;
using Annium.Finance.Providers.Tests.Lib.Infrastructure;
using Annium.Testing;
using Xunit;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Tests.Internal.User.Contracts.Converters;

/// <summary>
/// Verifies that <c>InitOrderResponseConverter</c> reads Binance's <c>POST /order</c> response - which
/// carries a <c>fills</c> array for the portion executed immediately - into an <see cref="OrderModel"/>,
/// deriving the average executed price from cumulative quote quantity over executed quantity rather than
/// from the fills directly.
/// </summary>
public class InitOrderResponseConverterTests : ProvidersTestBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InitOrderResponseConverterTests"/> class.
    /// </summary>
    /// <param name="outputHelper">The xUnit output helper to route trace logging to.</param>
    public InitOrderResponseConverterTests(ITestOutputHelper outputHelper)
        : base(outputHelper) { }

    /// <summary>
    /// Registers the Binance Spot provider so the converter under test is resolved from its actual registration.
    /// </summary>
    /// <param name="ctx">The fluent context to register providers into.</param>
    protected override void RegisterProvider(ProviderRegistrationContext ctx)
    {
        ctx.WithBinanceSpot();
    }

    /// <summary>
    /// A captured order-placement response for a partially filled take-profit-limit sell, with two fills,
    /// is parsed into an <see cref="OrderModel"/> whose executed price matches the fills' weighted average.
    /// </summary>
    [Fact]
    public void Success()
    {
        // arrange
        var raw =
            @"{
            ""symbol"": ""BTCUSDT"",
            ""orderId"": 28,
            ""orderListId"": -1,
            ""clientOrderId"": ""a9563c59-7bb1-4f59-bc25-35d30443cec1"",
            ""transactTime"": 1507725178599,
            ""price"": ""10019.70000000"",
            ""stopPrice"": ""10015.50000000"",
            ""origQty"": ""10.50000000"",
            ""executedQty"": ""6.40000000"",
            ""cummulativeQuoteQty"": ""64172.80000000"",
            ""status"": ""PARTIALLY_FILLED"",
            ""timeInForce"": ""GTC"",
            ""type"": ""TAKE_PROFIT_LIMIT"",
            ""side"": ""SELL"",
            ""workingTime"": 1507725176595,
            ""selfTradePreventionMode"": ""NONE"",
            ""fills"": [
                {
                    ""price"": ""10019.50000000"",
                    ""qty"": ""1.60000000"",
                    ""commission"": ""2.60000000"",
                    ""commissionAsset"": ""USDT"",
                    ""tradeId"": 56
                },
                {
                    ""price"": ""10029.50000000"",
                    ""qty"": ""4.8000000"",
                    ""commission"": ""7.800000"",
                    ""commissionAsset"": ""USDT"",
                    ""tradeId"": 57
                }
            ]
        }";

        // act
        var serializer = this.GetJsonSerializer(Constants.InitOrderKey);
        var deserialized = serializer.Deserialize<OrderModel>(Encoding.UTF8.GetBytes(raw)).NotNull();

        // assert
        deserialized.Id.Is("28");
        deserialized.ClientOrderId.Is("a9563c59-7bb1-4f59-bc25-35d30443cec1");
        deserialized.Symbol.Is("BTCUSDT");
        deserialized.Side.Is(OrderSide.Sell);
        deserialized.Type.Is(OrderType.TakeProfitLimit);
        deserialized.TotalQty.Is(10.5m);
        deserialized.Price.Is(10019.7m);
        deserialized.LevelPrice.Is(10015.5m);
        deserialized.Status.Is(OrderStatus.PartiallyFilled);
        deserialized.ExecutedQty.Is(6.4m);
        deserialized.ExecutedPrice.Is(10027m);
        deserialized.CreatedAt.Is(1507725176595);
        deserialized.UpdatedAt.Is(1507725178599);
    }
}
