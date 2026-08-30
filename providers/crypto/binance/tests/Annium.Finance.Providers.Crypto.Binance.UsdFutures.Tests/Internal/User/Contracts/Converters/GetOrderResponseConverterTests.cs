using System.Text;
using Annium.Finance.Providers.Abstractions.Domain.User;
using Annium.Finance.Providers.Core;
using Annium.Finance.Providers.Tests.Lib;
using Annium.Finance.Providers.Tests.Lib.Infrastructure;
using Annium.Testing;
using Xunit;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Tests.Internal.User.Contracts.Converters;

/// <summary>
/// Verifies that <c>GetOrderResponseConverter</c> reads Binance's <c>GET /order</c> response into an
/// <see cref="OrderModel"/>, notably taking the executed price straight from the futures-only <c>avgPrice</c>
/// field rather than deriving it, unlike the Spot converter.
/// </summary>
public class GetOrderResponseConverterTests : ProvidersTestBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetOrderResponseConverterTests"/> class.
    /// </summary>
    /// <param name="outputHelper">The xUnit output helper to route trace logging to.</param>
    public GetOrderResponseConverterTests(ITestOutputHelper outputHelper)
        : base(outputHelper) { }

    /// <summary>
    /// Registers the Binance USD-M futures provider so the converter under test is resolved from its actual registration.
    /// </summary>
    /// <param name="ctx">The fluent context to register providers into.</param>
    protected override void RegisterProvider(ProviderRegistrationContext ctx)
    {
        ctx.WithBinanceUsdFutures();
    }

    /// <summary>
    /// A captured order response for a partially filled reduce-only take-profit sell is parsed into an
    /// <see cref="OrderModel"/>, with the executed price taken from <c>avgPrice</c>.
    /// </summary>
    [Fact]
    public void Works()
    {
        // arrange
        var raw =
            @"  {
            ""avgPrice"": ""10700.30000"",
            ""clientOrderId"": ""4a1f8bb3-724f-462e-9bde-6d0120381ddd"",
            ""cumQuote"": ""100000.500"",
            ""executedQty"": ""3.1"",
            ""orderId"": 1917641,
            ""origQty"": ""10.50"",
            ""origType"": ""TAKE_PROFIT"",
            ""price"": ""10200.4"",
            ""reduceOnly"": true,
            ""side"": ""SELL"",
            ""positionSide"": ""SHORT"",
            ""status"": ""PARTIALLY_FILLED"",
            ""stopPrice"": ""9300.5"",
            ""closePosition"": false,
            ""symbol"": ""BTCUSDT"",
            ""time"": 1579276756075,
            ""timeInForce"": ""GTC"",
            ""type"": ""TAKE_PROFIT"",
            ""activatePrice"": ""9020"",
            ""priceRate"": ""0.3"",
            ""updateTime"": 1579276756076,
            ""workingType"": ""CONTRACT_PRICE"",
            ""priceProtect"": false
        }";

        // act
        var serializer = this.GetJsonSerializer(Constants.GetOrderKey);
        var deserialized = serializer.Deserialize<OrderModel>(Encoding.UTF8.GetBytes(raw)).NotNull();

        // assert
        deserialized.Id.Is("1917641");
        deserialized.ClientOrderId.Is("4a1f8bb3-724f-462e-9bde-6d0120381ddd");
        deserialized.Symbol.Is("BTCUSDT");
        deserialized.Side.Is(OrderSide.Sell);
        deserialized.Type.Is(OrderType.TakeProfitLimit);
        deserialized.TotalQty.Is(10.5m);
        deserialized.Price.Is(10200.4m);
        deserialized.LevelPrice.Is(9300.5m);
        deserialized.ReduceOnly.IsTrue();
        deserialized.CreatedAt.Is(1579276756075);
        deserialized.Status.Is(OrderStatus.PartiallyFilled);
        deserialized.ExecutedQty.Is(3.1m);
        deserialized.ExecutedPrice.Is(10700.3m);
        deserialized.UpdatedAt.Is(1579276756076);
    }
}
