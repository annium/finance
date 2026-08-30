using System;
using System.Text;
using Annium.Finance.Providers.Core;
using Annium.Finance.Providers.Crypto.Binance.Base.User.Contracts.Domain;
using Annium.Finance.Providers.Tests.Lib;
using Annium.Finance.Providers.Tests.Lib.Infrastructure;
using Annium.Testing;
using Xunit;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Tests.Internal.User.Contracts.Converters;

/// <summary>
/// Verifies that <c>CancelOrderResponseConverter</c> reads Binance's <c>DELETE /order</c> response into a
/// <see cref="CancelOrderResponse"/>, parsing the echoed <c>clientOrderId</c> as a <see cref="Guid"/> for a
/// cancelled trailing-stop order.
/// </summary>
public class CancelOrderResponseConverterTests : ProvidersTestBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CancelOrderResponseConverterTests"/> class.
    /// </summary>
    /// <param name="outputHelper">The xUnit output helper to route trace logging to.</param>
    public CancelOrderResponseConverterTests(ITestOutputHelper outputHelper)
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
    /// A captured cancel-order response for a trailing-stop order is parsed into the client order id (as a
    /// GUID) and the numeric order id.
    /// </summary>
    [Fact]
    public void Success()
    {
        // arrange
        var raw =
            @"{
            ""clientOrderId"": ""498cd707-dcce-4521-88ec-4ebded37f2ec"",
            ""cumQty"": ""0"",
            ""cumQuote"": ""0"",
            ""executedQty"": ""0"",
            ""orderId"": 283194212,
            ""origQty"": ""11"",
            ""origType"": ""TRAILING_STOP_MARKET"",
            ""price"": ""0"",
            ""reduceOnly"": false,
            ""side"": ""BUY"",
            ""positionSide"": ""SHORT"",
            ""status"": ""CANCELED"",
            ""stopPrice"": ""9300"",
            ""closePosition"": false,
            ""symbol"": ""BTCUSDT"",
            ""timeInForce"": ""GTC"",
            ""type"": ""TRAILING_STOP_MARKET"",
            ""activatePrice"": ""9020"",
            ""priceRate"": ""0.3"",
            ""updateTime"": 1571110484038,
            ""workingType"": ""CONTRACT_PRICE"",
            ""priceProtect"": false,
            ""priceMatch"": ""NONE"",
            ""selfTradePreventionMode"": ""NONE"",
            ""goodTillDate"": 0
        }";

        // act
        var serializer = this.GetJsonSerializer(Constants.CancelOrderKey);
        var deserialized = serializer.Deserialize<CancelOrderResponse>(Encoding.UTF8.GetBytes(raw)).NotNull();

        // assert
        deserialized.Id.Is(Guid.Parse("498cd707-dcce-4521-88ec-4ebded37f2ec"));
        deserialized.OrderId.Is("283194212");
    }
}
