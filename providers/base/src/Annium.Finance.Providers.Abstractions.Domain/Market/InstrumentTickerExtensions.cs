using System.Runtime.CompilerServices;

namespace Annium.Finance.Providers.Abstractions.Domain.Market;

/// <summary>
/// Provides pricing helpers for <see cref="InstrumentTicker"/>.
/// </summary>
public static class InstrumentTickerExtensions
{
    /// <summary>Gets the mid price between the current bid and ask.</summary>
    /// <param name="ticker">The ticker to derive the mid price from.</param>
    /// <returns>The average of <see cref="InstrumentTicker.BidPrice"/> and <see cref="InstrumentTicker.AskPrice"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static decimal Price(this InstrumentTicker ticker) => (ticker.BidPrice + ticker.AskPrice) / 2;
}
