using System.Text;
using Annium.Finance.Providers.Abstractions.Domain.User;
using Annium.Finance.Providers.Core;
using Annium.Finance.Providers.Tests.Lib;
using Annium.Finance.Providers.Tests.Lib.Infrastructure;
using Annium.Testing;
using Xunit;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Tests.Internal.User.Contracts.Converters;

public class ModifyOrderSuccessResponseConverterTests : ProvidersTestBase
{
    public ModifyOrderSuccessResponseConverterTests(ITestOutputHelper outputHelper)
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
            ""cancelResult"": ""SUCCESS"",
            ""newOrderResult"": ""SUCCESS"",
            ""cancelResponse"": {
                ""symbol"": ""BTCUSDT"",
                ""origClientOrderId"": ""DnLo3vTAQcjha43lAZhZ0y"",
                ""orderId"": 9,
                ""orderListId"": -1,
                ""clientOrderId"": ""a9563c59-7bb1-4f59-bc25-35d30443cec1"",
                ""transactTime"": 1684804350068,
                ""price"": ""0.01000000"",
                ""origQty"": ""0.000100"",
                ""executedQty"": ""0.00000000"",
                ""cummulativeQuoteQty"": ""0.00000000"",
                ""status"": ""CANCELED"",
                ""timeInForce"": ""GTC"",
                ""type"": ""LIMIT"",
                ""side"": ""SELL"",
                ""selfTradePreventionMode"": ""NONE""
            },
            ""newOrderResponse"": {
                ""symbol"": ""BTCUSDT"",
                ""orderId"": 10,
                ""orderListId"": -1,
                ""clientOrderId"": ""a9563c59-7bb1-4f59-bc25-35d30443cec1"",
                ""transactTime"": 1652928801803,
                ""price"": ""0.02000000"",
                ""origQty"": ""0.040000"",
                ""executedQty"": ""0.00000000"",
                ""cummulativeQuoteQty"": ""0.00000000"",
                ""status"": ""NEW"",
                ""timeInForce"": ""GTC"",
                ""type"": ""LIMIT"",
                ""side"": ""BUY"",
                ""workingTime"": 1669277163808,
                ""fills"": [],
                ""selfTradePreventionMode"": ""NONE""
            }
        }";

        // act
        var serializer = this.GetJsonSerializer(Constants.ModifyOrderKey);
        var deserialized = serializer.Deserialize<OrderModel>(Encoding.UTF8.GetBytes(raw)).NotNull();

        // assert
        deserialized.Id.Is("10");
        deserialized.ClientOrderId.Is("a9563c59-7bb1-4f59-bc25-35d30443cec1");
        deserialized.Symbol.Is("BTCUSDT");
        deserialized.Side.Is(OrderSide.Buy);
        deserialized.Type.Is(OrderType.Limit);
        deserialized.TotalQty.Is(0.04m);
        deserialized.Price.Is(0.02m);
        deserialized.LevelPrice.Is(0);
        deserialized.Status.Is(OrderStatus.New);
        deserialized.ExecutedQty.Is(0);
        deserialized.ExecutedPrice.Is(0);
        deserialized.CreatedAt.Is(1669277163808);
        deserialized.UpdatedAt.Is(1652928801803);
    }
}
