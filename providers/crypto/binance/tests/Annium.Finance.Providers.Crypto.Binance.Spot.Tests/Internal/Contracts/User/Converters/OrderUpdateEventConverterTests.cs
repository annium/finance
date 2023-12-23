using System.Text;
using Annium.Finance.Providers.Abstractions.Domain.Enums;
using Annium.Finance.Providers.Crypto.Binance.Spot.Internal.Contracts.User.Domain;
using Annium.Finance.Providers.Tests.Shared.Connectors;
using Annium.Finance.Providers.Tests.Shared.Extensions;
using Annium.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Tests.Internal.Contracts.User.Converters;

public class OrderUpdateEventConverterTests : ConnectorTestBase
{
    public OrderUpdateEventConverterTests(ITestOutputHelper outputHelper)
        : base(ctx => ctx.WithBinanceSpot(), outputHelper) { }

    [Fact]
    public void Works()
    {
        // arrange
        var raw =
            @"{
            ""e"": ""executionReport"",
            ""E"": 1499405658658,
            ""s"": ""ETHBTC"",
            ""c"": ""mUvoqJxFIILMdfAW5iGSOW"",
            ""S"": ""BUY"",
            ""o"": ""LIMIT"",
            ""f"": ""GTC"",
            ""q"": ""1.70000000"",
            ""p"": ""0.10264410"",
            ""P"": ""0.30000000"",
            ""F"": ""0.00000000"",
            ""g"": -1,
            ""C"": """",
            ""x"": ""NEW"",
            ""X"": ""NEW"",
            ""r"": ""NONE"",
            ""i"": 4293153,
            ""l"": ""2.10000000"",
            ""z"": ""1.20000000"",
            ""L"": ""3.30000000"",
            ""n"": ""5.4"",
            ""N"": ""BTC"",
            ""T"": 1499405658677,
            ""t"": 123123,
            ""I"": 8641984,
            ""w"": true,
            ""m"": true,
            ""M"": false,
            ""O"": 1499405658657,
            ""Z"": ""2.40000000"",
            ""Y"": ""0.00000000"",
            ""Q"": ""0.00000000"",
            ""W"": 1499405658657,
            ""V"": ""NONE""
        }";

        // act - deserialize
        var serializer = this.GetJsonSerializer(Constants.OrderUpdateKey);
        var deserialized = serializer.Deserialize<OrderUpdateEvent>(Encoding.UTF8.GetBytes(raw)).NotNull();

        // assert - deserialization
        deserialized.Symbol.Is("ETHBTC");
        deserialized.TradeId.Is("123123");
        deserialized.OrderId.Is("4293153");
        deserialized.ClientOrderId.Is("mUvoqJxFIILMdfAW5iGSOW");
        deserialized.Type.Is(OrderType.Limit);
        deserialized.Side.Is(OrderSide.Buy);
        deserialized.TotalQty.Is(1.7m);
        deserialized.Price.Is(0.10264410m);
        deserialized.LevelPrice.Is(0.3m);
        deserialized.Status.Is(OrderStatus.New);
        deserialized.ExecutedQty.Is(1.2m);
        deserialized.ExecutedPrice.Is(2m);
        deserialized.LastExecutedQty.Is(2.1m);
        deserialized.LastExecutedPrice.Is(3.3m);
        deserialized.CommissionAsset.Is("BTC");
        deserialized.CommissionAmount.Is(5.4m);
        deserialized.IsMaker.IsTrue();
        deserialized.CreatedDate.Is(1499405658657);
        deserialized.UpdatedDate.Is(1499405658677);
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
