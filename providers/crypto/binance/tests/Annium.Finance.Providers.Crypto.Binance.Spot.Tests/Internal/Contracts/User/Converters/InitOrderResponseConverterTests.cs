using System.Text;
using Annium.Finance.Providers.Abstractions.Domain.User;
using Annium.Finance.Providers.Shared;
using Annium.Finance.Providers.Tests.Lib;
using Annium.Finance.Providers.Tests.Lib.TestBaseExtensions;
using Annium.Testing;
using Xunit;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Tests.Internal.Contracts.User.Converters;

public class InitOrderResponseConverterTests : ProvidersTestBase
{
    public InitOrderResponseConverterTests(ITestOutputHelper outputHelper)
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
