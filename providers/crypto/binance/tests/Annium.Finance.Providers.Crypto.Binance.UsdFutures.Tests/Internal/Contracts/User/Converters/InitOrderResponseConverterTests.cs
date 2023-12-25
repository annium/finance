using System.Text;
using Annium.Finance.Providers.Abstractions.Domain.Dto;
using Annium.Finance.Providers.Abstractions.Domain.Enums;
using Annium.Finance.Providers.Tests.Shared.Connectors;
using Annium.Finance.Providers.Tests.Shared.Extensions;
using Annium.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Tests.Internal.Contracts.User.Converters;

public class InitOrderResponseConverterTests : ConnectorTestBase
{
    public InitOrderResponseConverterTests(ITestOutputHelper outputHelper)
        : base(ctx => ctx.WithBinanceUsdFutures(), outputHelper) { }

    [Fact]
    public void Success()
    {
        // arrange
        var raw =
            @"{
            ""orderId"": 20072994037,
            ""symbol"": ""BTCUSDT"",
            ""pair"": ""BTCUSDT"",
            ""status"": ""PARTIALLY_FILLED"",
            ""clientOrderId"": ""a9563c59-7bb1-4f59-bc25-35d30443cec1"",
            ""price"": ""10019.7"",
            ""avgPrice"": ""10027"",
            ""origQty"": ""10.5"",
            ""executedQty"": ""6.4"",
            ""cumQty"": ""0"",
            ""cumBase"": ""0"",
            ""timeInForce"": ""GTC"",
            ""type"": ""LIMIT"",
            ""reduceOnly"": true,
            ""closePosition"": false,
            ""side"": ""BUY"",
            ""positionSide"": ""LONG"",
            ""stopPrice"": ""10015.5"",
            ""workingType"": ""CONTRACT_PRICE"",
            ""priceProtect"": false,
            ""origType"": ""LIMIT"",
            ""updateTime"": 1629182711600
        }";

        // act
        var serializer = this.GetJsonSerializer(Constants.InitOrderKey);
        var deserialized = serializer.Deserialize<OrderDto>(Encoding.UTF8.GetBytes(raw)).NotNull();

        // assert
        deserialized.Id.Is("20072994037");
        deserialized.ClientOrderId.Is("a9563c59-7bb1-4f59-bc25-35d30443cec1");
        deserialized.Symbol.Is("BTCUSDT");
        deserialized.Side.Is(OrderSide.Buy);
        deserialized.Type.Is(OrderType.Limit);
        deserialized.TotalQty.Is(10.5m);
        deserialized.Price.Is(10019.7m);
        deserialized.LevelPrice.Is(10015.5m);
        deserialized.ReduceOnly.IsTrue();
        deserialized.CreatedAt.Is(1629182711600);
        deserialized.Status.Is(OrderStatus.PartiallyFilled);
        deserialized.ExecutedQty.Is(6.4m);
        deserialized.ExecutedPrice.Is(10027m);
        deserialized.UpdatedAt.Is(1629182711600);
    }
}
