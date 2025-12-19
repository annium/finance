using System;
using System.Text;
using Annium.Finance.Providers.Core;
using Annium.Finance.Providers.Crypto.Binance.Base.Contracts.User.Domain;
using Annium.Finance.Providers.Tests.Lib;
using Annium.Finance.Providers.Tests.Lib.Infrastructure;
using Annium.Testing;
using Xunit;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Tests.Internal.Contracts.User.Converters;

public class CancelOrderResponseConverterTests : ProvidersTestBase
{
    public CancelOrderResponseConverterTests(ITestOutputHelper outputHelper)
        : base(outputHelper) { }

    protected override void RegisterProvider(ProviderRegistrationContext ctx)
    {
        ctx.WithBinanceSpot();
    }

    [Fact]
    public void Success()
    {
        // arrange
        var raw =
            @"{
            ""symbol"": ""LTCBTC"",
            ""origClientOrderId"": ""myOrder1"",
            ""orderId"": 4,
            ""orderListId"": -1,
            ""clientOrderId"": ""c5a7733c-ad87-4c6d-a2a4-941ee8e0f7f7"",
            ""transactTime"": 1684804350068,
            ""price"": ""2.00000000"",
            ""origQty"": ""1.00000000"",
            ""executedQty"": ""0.00000000"",
            ""cummulativeQuoteQty"": ""0.00000000"",
            ""status"": ""CANCELED"",
            ""timeInForce"": ""GTC"",
            ""type"": ""LIMIT"",
            ""side"": ""BUY"",
            ""selfTradePreventionMode"": ""NONE""
        }";

        // act
        var serializer = this.GetJsonSerializer(Constants.CancelOrderKey);
        var deserialized = serializer.Deserialize<CancelOrderResponse>(Encoding.UTF8.GetBytes(raw)).NotNull();

        // assert
        deserialized.OrderId.Is("4");
        deserialized.Id.Is(Guid.Parse("c5a7733c-ad87-4c6d-a2a4-941ee8e0f7f7"));
    }
}
