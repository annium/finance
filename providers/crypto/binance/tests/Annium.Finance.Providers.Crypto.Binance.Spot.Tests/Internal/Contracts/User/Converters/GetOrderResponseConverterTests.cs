using System.Text;
using Annium.Finance.Providers.Abstractions.Domain.Enums;
using Annium.Finance.Providers.Abstractions.Domain.Models;
using Annium.Finance.Providers.Tests.Shared.Connectors;
using Annium.Finance.Providers.Tests.Shared.Extensions;
using Annium.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Tests.Internal.Contracts.User.Converters;

public class GetOrderResponseConverterTests : ConnectorTestBase
{
    public GetOrderResponseConverterTests(ITestOutputHelper outputHelper)
        : base(ctx => ctx.WithBinanceSpot(), outputHelper) { }

    [Fact]
    public void Works()
    {
        // arrange
        var raw =
            @" {
            ""symbol"": ""BTCUSDT"",
            ""orderId"": 28,
            ""orderListId"": -1,
            ""clientOrderId"": ""4a1f8bb3-724f-462e-9bde-6d0120381ddd"",
            ""price"": ""10019.7"",
            ""origQty"": ""10.5"",
            ""executedQty"": ""6.4"",
            ""cummulativeQuoteQty"": ""12.8"",
            ""status"": ""PARTIALLY_FILLED"",
            ""timeInForce"": ""GTC"",
            ""type"": ""TAKE_PROFIT_LIMIT"",
            ""side"": ""SELL"",
            ""stopPrice"": ""10015.5"",
            ""icebergQty"": ""0.0"",
            ""time"": 1499827319559,
            ""updateTime"": 1499827319579,
            ""isWorking"": true,
            ""origQuoteOrderQty"": ""0.000000"",
            ""workingTime"": 1499827319559,
            ""selfTradePreventionMode"": ""NONE""
        }";

        // act
        var serializer = this.GetJsonSerializer(Constants.GetOrderKey);
        var deserialized = serializer.Deserialize<OrderModel>(Encoding.UTF8.GetBytes(raw)).NotNull();

        // assert
        deserialized.Id.Is("28");
        deserialized.ClientOrderId.Is("4a1f8bb3-724f-462e-9bde-6d0120381ddd");
        deserialized.Symbol.Is("BTCUSDT");
        deserialized.Side.Is(OrderSide.Sell);
        deserialized.Type.Is(OrderType.TakeProfitLimit);
        deserialized.TotalQty.Is(10.5m);
        deserialized.Price.Is(10019.7m);
        deserialized.LevelPrice.Is(10015.5m);
        deserialized.CreatedAt.Is(1499827319559);
        deserialized.Status.Is(OrderStatus.PartiallyFilled);
        deserialized.ExecutedQty.Is(6.4m);
        deserialized.ExecutedPrice.Is(2m);
        deserialized.UpdatedAt.Is(1499827319579);
    }
}
