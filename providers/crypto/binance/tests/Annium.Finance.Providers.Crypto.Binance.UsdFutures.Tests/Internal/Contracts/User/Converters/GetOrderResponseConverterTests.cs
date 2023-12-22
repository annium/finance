using System;
using System.Text;
using Annium.Finance.Providers.Abstractions.Domain.Enums;
using Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Contracts.User.Domain;
using Annium.Finance.Providers.Tests.Shared.Connectors;
using Annium.Finance.Providers.Tests.Shared.Extensions;
using Annium.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Tests.Internal.Contracts.User.Converters;

public class GetOrderResponseConverterTests : ConnectorTestBase
{
    public GetOrderResponseConverterTests(ITestOutputHelper outputHelper)
        : base(ctx => ctx.WithBinanceUsdFutures(), outputHelper) { }

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
        var deserialized = serializer.Deserialize<OrderResponse>(Encoding.UTF8.GetBytes(raw)).NotNull();

        // assert
        deserialized.Id.Is(Guid.Parse("4a1f8bb3-724f-462e-9bde-6d0120381ddd"));
        deserialized.OrderId.Is("1917641");
        deserialized.Symbol.Is("BTCUSDT");
        deserialized.Side.Is(OrderSide.Sell);
        deserialized.Type.Is(OrderType.TakeProfitLimit);
        deserialized.TotalQty.Is(10.5m);
        deserialized.Price.Is(10200.4m);
        deserialized.LevelPrice.Is(9300.5m);
        deserialized.CreatedAt.Is(1579276756075);
        deserialized.Status.Is(OrderStatus.PartiallyFilled);
        deserialized.ExecutedQty.Is(3.1m);
        deserialized.ExecutedPrice.Is(10700.3m);
        deserialized.UpdatedAt.Is(1579276756076);
    }
}
