using System;
using System.Text;
using Annium.Finance.Providers.Core;
using Annium.Finance.Providers.Crypto.Binance.Base.Contracts.User.Domain;
using Annium.Finance.Providers.Tests.Lib;
using Annium.Finance.Providers.Tests.Lib.Infrastructure;
using Annium.Testing;
using Xunit;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Tests.Internal.Contracts.User.Converters;

public class CancelOrderResponseConverterTests : ProvidersTestBase
{
    public CancelOrderResponseConverterTests(ITestOutputHelper outputHelper)
        : base(outputHelper) { }

    protected override void RegisterProvider(ProviderRegistrationContext ctx)
    {
        ctx.WithBinanceUsdFutures();
    }

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
