using System.Text;
using Annium.Finance.Providers.Abstractions.Domain.Enums;
using Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Contracts.User.Domain;
using Annium.Finance.Providers.Tests.Lib;
using Annium.Finance.Providers.Tests.Lib.Extensions;
using Annium.Testing;
using Xunit;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Tests.Internal.Contracts.User.Converters;

public class OrderUpdateEventConverterTests : ProvidersTestBase
{
    public OrderUpdateEventConverterTests(ITestOutputHelper outputHelper)
        : base(ctx => ctx.WithBinanceUsdFutures(), outputHelper) { }

    [Fact]
    public void Works()
    {
        // arrange
        var raw =
            @"{
            ""e"": ""ORDER_TRADE_UPDATE"",
            ""E"": 1499405658678,
            ""T"": 1499405658677,
            ""o"": {
                ""s"": ""BTCUSDT"",
                ""c"": ""mUvoqJxFIILMdfAW5iGSOW"",
                ""S"": ""SELL"",
                ""o"": ""TRAILING_STOP_MARKET"",
                ""f"": ""GTC"",
                ""q"": ""1.7"",
                ""p"": ""10264.410"",
                ""ap"": ""12305.6"",
                ""sp"": ""7103.04"",
                ""x"": ""NEW"",
                ""X"": ""PARTIALLY_FILLED"",
                ""i"": 8886774,
                ""l"": ""2.4"",
                ""z"": ""10.5"",
                ""L"": ""11742.3"",
                ""N"": ""USDT"",
                ""n"": ""3.6"",
                ""T"": 1499405658657,
                ""t"": 123123,
                ""b"": ""0"",
                ""a"": ""9.91"",
                ""m"": true,
                ""R"": true,
                ""wt"": ""CONTRACT_PRICE"",
                ""ot"": ""TRAILING_STOP_MARKET"",
                ""ps"": ""LONG"",
                ""cp"": false,
                ""AP"": ""7476.89"",
                ""cr"": ""5.0"",
                ""pP"": false,
                ""si"": 0,
                ""ss"": 0,
                ""rp"": ""0""
            }
        }";

        // act - deserialize
        var serializer = this.GetJsonSerializer(Constants.OrderUpdateKey);
        var deserialized = serializer.Deserialize<OrderUpdateEvent>(Encoding.UTF8.GetBytes(raw));

        // assert - deserialization
        deserialized.Symbol.Is("BTCUSDT");
        deserialized.TradeId.Is("123123");
        deserialized.OrderId.Is("8886774");
        deserialized.ClientOrderId.Is("mUvoqJxFIILMdfAW5iGSOW");
        deserialized.Type.Is(OrderType.StopLossMarket);
        deserialized.Side.Is(OrderSide.Sell);
        deserialized.TotalQty.Is(1.7m);
        deserialized.Price.Is(10264.410m);
        deserialized.LevelPrice.Is(7103.04m);
        deserialized.ReduceOnly.IsTrue();
        deserialized.Status.Is(OrderStatus.PartiallyFilled);
        deserialized.ExecutedQty.Is(10.5m);
        deserialized.ExecutedPrice.Is(12305.6m);
        deserialized.LastExecutedQty.Is(2.4m);
        deserialized.LastExecutedPrice.Is(11742.3m);
        deserialized.CommissionAsset.Is("USDT");
        deserialized.CommissionAmount.Is(3.6m);
        deserialized.IsMaker.IsTrue();
        deserialized.CreatedAt.Is(0);
        deserialized.UpdatedAt.Is(1499405658657);
    }

    [Fact]
    public void SkipsInvalidData()
    {
        // arrange
        var raw =
            @"{
            ""e"": ""invalid""
        }";

        // act - deserialize
        var serializer = this.GetJsonSerializer(Constants.OrderUpdateKey);
        var deserialized = serializer.Deserialize<OrderUpdateEvent>(Encoding.UTF8.GetBytes(raw));

        // assert - deserialization
        deserialized.IsDefault();
    }
}
