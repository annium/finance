using System.Text;
using Annium.Finance.Providers.Abstractions.Domain.Models;
using Annium.Finance.Providers.Crypto.Binance.Base.Contracts.Shared.Domain;
using Annium.Finance.Providers.Shared;
using Annium.Finance.Providers.Tests.Lib;
using Annium.Finance.Providers.Tests.Lib.TestBaseExtensions;
using Annium.Testing;
using Xunit;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Tests.Internal.Contracts.Shared.Converters;

public class StreamDataTests : ProvidersTestBase
{
    public StreamDataTests(ITestOutputHelper outputHelper)
        : base(outputHelper) { }

    protected override void RegisterProvider(ProviderRegistrationContext ctx)
    {
        ctx.WithBinanceUsdFutures();
    }

    [Fact]
    public void Works()
    {
        // arrange
        var raw =
            @"{
            ""stream"": ""btcusdt@bookTicker"",
            ""data"": {
                ""u"":17242169,
                ""s"":""BTCUSDT"",
                ""b"":""9548.1"",
                ""B"":""52"",
                ""a"":""9548.5"",
                ""A"":""11""
            }
        }";

        // act - deserialize
        var serializer = this.GetJsonSerializer(Constants.InstrumentTickerKey);
        var deserialized = serializer.Deserialize<StreamData<InstrumentTicker>>(Encoding.UTF8.GetBytes(raw)).NotNull();

        // assert - deserialization
        deserialized.Name.Is("btcusdt@bookTicker");
        deserialized.Data.Symbol.Is("BTCUSDT");
        deserialized.Data.BidPrice.Is(9548.1m);
        deserialized.Data.AskPrice.Is(9548.5m);
    }

    [Fact]
    public void SkipsInvalidData()
    {
        // arrange
        var raw =
            @"{
            ""stream"": ""btcusdt@bookTicker"",
            ""data"": {
                ""u"":17242169,
                ""b"":""9548.1"",
                ""B"":""52"",
                ""a"":""9548.5"",
                ""A"":""11""
            }
        }";

        // act - deserialize
        var serializer = this.GetJsonSerializer(Constants.InstrumentTickerKey);
        var deserialized = serializer.Deserialize<StreamData<InstrumentTicker>>(Encoding.UTF8.GetBytes(raw));

        // assert - deserialization
        deserialized.IsDefault();
    }
}
