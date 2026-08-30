using System.Collections.Generic;
using System.Text;
using Annium.Finance.Providers.Abstractions.Domain.Market;
using Annium.Finance.Providers.Core;
using Annium.Finance.Providers.Tests.Lib;
using Annium.Finance.Providers.Tests.Lib.Infrastructure;
using Annium.Testing;
using Xunit;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Tests.Internal.Market.Contracts.Converters;

/// <summary>
/// Verifies that <c>CandleConverter</c> reads Binance's kline array shape - each candle a JSON array of
/// <c>[open time, open, high, low, close, volume, ...]</c> - into a <see cref="CandleModel"/>, and that an
/// entry too short to contain the fields it needs deserializes to a default element rather than throwing.
/// </summary>
public class CandleConverterTests : ProvidersTestBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CandleConverterTests"/> class.
    /// </summary>
    /// <param name="outputHelper">The xUnit output helper to route trace logging to.</param>
    public CandleConverterTests(ITestOutputHelper outputHelper)
        : base(outputHelper) { }

    /// <summary>
    /// Registers the Binance USD-M futures provider so the converter under test is resolved from its actual registration.
    /// </summary>
    /// <param name="ctx">The fluent context to register providers into.</param>
    protected override void RegisterProvider(ProviderRegistrationContext ctx)
    {
        ctx.WithBinanceUsdFutures();
    }

    /// <summary>
    /// A captured kline array - open time, OHLC prices and volume as strings - is parsed into a
    /// <see cref="CandleModel"/> with the numeric strings converted to <see cref="decimal"/>.
    /// </summary>
    [Fact]
    public void Works()
    {
        // arrange
        var raw =
            @"[
          [
            1499040000000,
            ""0.01634790"",
            ""0.80000000"",
            ""0.01575800"",
            ""0.01577100"",
            ""148976.11427815""
          ]
        ]";

        // act - deserialize
        var serializer = this.GetJsonSerializer(Constants.CandleKey);
        var deserialized = serializer.Deserialize<List<CandleModel>>(Encoding.UTF8.GetBytes(raw)).NotNull();

        // assert - deserialization
        deserialized.Has(1);
        deserialized
            .At(0)
            .IsEqual(
                new CandleModel(1499040000000, 0.01634790m, 0.80000000m, 0.01575800m, 0.01577100m, 148976.11427815m)
            );
    }

    /// <summary>
    /// A candle entry with too few elements to contain OHLCV data deserializes to a default
    /// <see cref="CandleModel"/> instead of throwing, so one bad entry doesn't fail the whole batch.
    /// </summary>
    [Fact]
    public void SkipsInvalidData()
    {
        // arrange
        var raw = "[[]]";

        // act - deserialize
        var serializer = this.GetJsonSerializer(Constants.CandleKey);
        var deserialized = serializer.Deserialize<List<CandleModel>>(Encoding.UTF8.GetBytes(raw));

        // assert - deserialization
        deserialized.Has(1);
        deserialized.At(0).IsDefault();
    }
}
