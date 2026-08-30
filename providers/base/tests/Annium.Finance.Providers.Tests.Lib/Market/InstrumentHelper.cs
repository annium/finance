using Annium.Finance.Providers.Abstractions.Domain.Shared;

namespace Annium.Finance.Providers.Tests.Lib.Market;

/// <summary>
/// Builds fake <see cref="Instrument"/> instances for tests that need one without going through a provider.
/// </summary>
public static class InstrumentHelper
{
    /// <summary>A ready-made BTC/USDT instrument for tests that don't care about its exact limits.</summary>
    public static readonly Instrument DefaultInstrument = CreateInstrument("BTC", "USDT", 0.1m, 0.01m);

    /// <summary>
    /// Creates a fake instrument for the given resource/currency pair, deriving its quantity and notional
    /// limits from the lot and tick size.
    /// </summary>
    /// <param name="resource">The base asset code (e.g. "BTC").</param>
    /// <param name="currency">The quote asset code (e.g. "USDT").</param>
    /// <param name="lotSize">The quantity step orders must be a multiple of.</param>
    /// <param name="tickSize">The price step order prices must be a multiple of.</param>
    /// <returns>A new fake instrument with the given resource, currency, lot size and tick size.</returns>
    public static Instrument CreateInstrument(string resource, string currency, decimal lotSize, decimal tickSize) =>
        new(
            "fake",
            ProviderEnvironment.Test,
            $"{resource}{currency}",
            lotSize,
            tickSize,
            lotSize * 10,
            lotSize * 100,
            lotSize * tickSize * 1000,
            decimal.MaxValue,
            int.MaxValue
        );
}
