using System.Text;
using Annium.Finance.Providers.Abstractions.Domain.Models;
using Annium.Finance.Providers.Tests.Shared.Connectors;
using Annium.Finance.Providers.Tests.Shared.Extensions;
using Annium.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Tests.Internal.Contracts.Market.Converters;

public class InstrumentTickerConverterTests : ConnectorTestBase
{
    public InstrumentTickerConverterTests(ITestOutputHelper outputHelper)
        : base(ctx => ctx.WithBinanceUsdFutures(), outputHelper) { }

    [Fact]
    public void Works()
    {
        // arrange
        var raw =
            @"{
            ""u"":17242169,
            ""s"":""BTCUSDT"",
            ""b"":""9548.1"",
            ""B"":""52"",
            ""a"":""9548.5"",
            ""A"":""11""
        }";

        // act
        var serializer = this.GetJsonSerializer(Constants.InstrumentTickerSerializerKey);
        var deserialized = serializer.Deserialize<InstrumentTicker>(Encoding.UTF8.GetBytes(raw)).NotNull();

        // assert
        deserialized.Symbol.Is("BTCUSDT");
        deserialized.BidPrice.Is(9548.1m);
        deserialized.AskPrice.Is(9548.5m);
    }

    [Fact]
    public void SkipsInvalidData()
    {
        // arrange
        var raw =
            @"{
            ""s"": """"
        }";

        // act - deserialize
        var serializer = this.GetJsonSerializer(Constants.InstrumentTickerSerializerKey);
        var deserialized = serializer.Deserialize<InstrumentTicker>(Encoding.UTF8.GetBytes(raw));

        // assert - deserialization
        deserialized.IsDefault();
    }
}
