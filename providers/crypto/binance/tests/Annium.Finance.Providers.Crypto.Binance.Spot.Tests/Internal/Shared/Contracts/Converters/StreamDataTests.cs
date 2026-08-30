using System.Text;
using Annium.Finance.Providers.Abstractions.Domain.Market;
using Annium.Finance.Providers.Core;
using Annium.Finance.Providers.Crypto.Binance.Base.Shared.Contracts.Domain;
using Annium.Finance.Providers.Tests.Lib;
using Annium.Finance.Providers.Tests.Lib.Infrastructure;
using Annium.Testing;
using Xunit;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Tests.Internal.Shared.Contracts.Converters;

/// <summary>
/// Verifies that <c>StreamData&lt;T&gt;</c>'s converter reads the <c>{stream, data}</c> envelope Binance
/// wraps every combined-stream WebSocket message in, deserializing <c>data</c> as the payload type <c>T</c>
/// (here <see cref="InstrumentTicker"/>), and that a payload whose <c>data</c> can't itself be parsed
/// deserializes the whole envelope to null instead of throwing.
/// </summary>
public class StreamDataTests : ProvidersTestBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StreamDataTests"/> class.
    /// </summary>
    /// <param name="outputHelper">The xUnit output helper to route trace logging to.</param>
    public StreamDataTests(ITestOutputHelper outputHelper)
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
    /// A combined-stream envelope is parsed into its stream name and its <c>data</c> payload, deserialized as
    /// the generic type argument.
    /// </summary>
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

    /// <summary>
    /// When the nested <c>data</c> payload doesn't parse as its declared type - here missing the ticker's
    /// symbol - the whole envelope deserializes to null instead of throwing.
    /// </summary>
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
