using System.Text;
using Annium.Finance.Providers.Abstractions.Domain.User;
using Annium.Finance.Providers.Core;
using Annium.Finance.Providers.Crypto.Binance.Spot.Internal.User.Contracts.Domain;
using Annium.Finance.Providers.Tests.Lib;
using Annium.Finance.Providers.Tests.Lib.Infrastructure;
using Annium.Testing;
using Xunit;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Tests.Internal.User.Contracts.Converters;

/// <summary>
/// Verifies that <c>OrderUpdateEventConverter</c> reads Binance's <c>executionReport</c> user-data stream
/// event - a heavily abbreviated field set covering both the order's cumulative state and the last
/// individual fill - into an <see cref="OrderUpdateEvent"/>, and that an event with a different <c>e</c>
/// type deserializes to null instead of throwing.
/// </summary>
public class OrderUpdateEventConverterTests : ProvidersTestBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OrderUpdateEventConverterTests"/> class.
    /// </summary>
    /// <param name="outputHelper">The xUnit output helper to route trace logging to.</param>
    public OrderUpdateEventConverterTests(ITestOutputHelper outputHelper)
        : base(outputHelper) { }

    /// <summary>
    /// Registers the Binance Spot provider so the converter under test is resolved from its actual registration.
    /// </summary>
    /// <param name="ctx">The fluent context to register providers into.</param>
    protected override void RegisterProvider(ProviderRegistrationContext ctx)
    {
        ctx.WithBinanceSpot();
    }

    /// <summary>
    /// A captured <c>executionReport</c> event is parsed into the order's identifiers, cumulative
    /// executed quantity/price, the last individual fill's quantity/price, and commission.
    /// </summary>
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
        deserialized.CreatedAt.Is(1499405658657);
        deserialized.UpdatedAt.Is(1499405658677);
    }

    /// <summary>
    /// An event whose <c>e</c> type tag isn't <c>executionReport</c> deserializes to null instead of throwing.
    /// </summary>
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
