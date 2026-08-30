using System.Text;
using Annium.Finance.Providers.Abstractions.Domain.Market;
using Annium.Finance.Providers.Core;
using Annium.Finance.Providers.Tests.Lib;
using Annium.Finance.Providers.Tests.Lib.Infrastructure;
using Annium.Testing;
using Xunit;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Tests.Internal.Market.Contracts.Market.Converters;

/// <summary>
/// Verifies that <c>InstrumentTickerConverter</c> reads Binance's book-ticker stream shape - abbreviated
/// <c>b</c>/<c>a</c> bid/ask price fields alongside their quantities - into an <see cref="InstrumentTicker"/>,
/// and that a payload missing the fields it needs deserializes to null instead of throwing.
/// </summary>
public class InstrumentTickerConverterTests : ProvidersTestBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InstrumentTickerConverterTests"/> class.
    /// </summary>
    /// <param name="outputHelper">The xUnit output helper to route trace logging to.</param>
    public InstrumentTickerConverterTests(ITestOutputHelper outputHelper)
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
    /// A captured book-ticker payload is parsed into its symbol and best bid/ask prices.
    /// </summary>
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
        var serializer = this.GetJsonSerializer(Constants.InstrumentTickerKey);
        var deserialized = serializer.Deserialize<InstrumentTicker>(Encoding.UTF8.GetBytes(raw)).NotNull();

        // assert
        deserialized.Symbol.Is("BTCUSDT");
        deserialized.BidPrice.Is(9548.1m);
        deserialized.AskPrice.Is(9548.5m);
    }

    /// <summary>
    /// A payload missing the fields the converter needs deserializes to null instead of throwing.
    /// </summary>
    [Fact]
    public void SkipsInvalidData()
    {
        // arrange
        var raw =
            @"{
            ""s"": """"
        }";

        // act - deserialize
        var serializer = this.GetJsonSerializer(Constants.InstrumentTickerKey);
        var deserialized = serializer.Deserialize<InstrumentTicker>(Encoding.UTF8.GetBytes(raw));

        // assert - deserialization
        deserialized.IsDefault();
    }
}
