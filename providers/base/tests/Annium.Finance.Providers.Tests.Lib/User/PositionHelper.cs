using System;
using System.Runtime.CompilerServices;
using Annium.Finance.Providers.Abstractions.Domain.User;
using Annium.Finance.Providers.Tests.Lib.Market;

namespace Annium.Finance.Providers.Tests.Lib.User;

/// <summary>
/// Builds fresh, empty fake <see cref="Position"/> instances for tests that need one without going through
/// a provider, and computes the volume-weighted average price positions use when a fill moves the price.
/// </summary>
public static class PositionHelper
{
    /// <summary>
    /// Creates an empty cross-margin position on <see cref="InstrumentHelper.DefaultInstrument"/> with the
    /// given leverage.
    /// </summary>
    /// <param name="leverage">The leverage multiplier to apply to the position.</param>
    /// <returns>A new, empty position.</returns>
    public static Position CreatePosition(decimal leverage) =>
        CreatePosition(InstrumentHelper.DefaultInstrument, leverage);

    /// <summary>
    /// Creates an empty cross-margin position on the given instrument with the given leverage.
    /// </summary>
    /// <param name="instrument">The instrument to open the position on.</param>
    /// <param name="leverage">The leverage multiplier to apply to the position.</param>
    /// <returns>A new, empty position.</returns>
    public static Position CreatePosition(Instrument instrument, decimal leverage) =>
        new(
            Guid.NewGuid(),
            instrument,
            0,
            OrientationRange.Both,
            MarginType.Cross,
            leverage,
            0,
            decimal.Zero,
            decimal.Zero,
            decimal.Zero,
            decimal.Zero,
            decimal.Zero,
            decimal.Zero,
            decimal.Zero,
            decimal.Zero,
            decimal.Zero,
            decimal.Zero,
            decimal.Zero,
            decimal.Zero
        );

    /// <summary>
    /// Computes the volume-weighted average price after blending in a new fill.
    /// </summary>
    /// <param name="currentQty">The quantity already held at the current price.</param>
    /// <param name="currentPrice">The current average price.</param>
    /// <param name="executedQty">The additional quantity filled.</param>
    /// <param name="executedPrice">The price the additional quantity was filled at.</param>
    /// <returns>The new volume-weighted average price, or zero if the resulting quantity is zero.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static decimal ResolvePrice(
        decimal currentQty,
        decimal currentPrice,
        decimal executedQty,
        decimal executedPrice
    )
    {
        var totalQty = currentQty + executedQty;
        if (totalQty == 0)
            return 0;

        return (currentQty * currentPrice + executedQty * executedPrice) / totalQty;
    }
}
