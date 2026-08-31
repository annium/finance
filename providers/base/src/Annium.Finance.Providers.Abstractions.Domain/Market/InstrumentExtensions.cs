using System;

namespace Annium.Finance.Providers.Abstractions.Domain.Market;

/// <summary>
/// Provides quantity and price rounding helpers that enforce an <see cref="IInstrument"/>'s lot size and tick size constraints.
/// </summary>
public static class InstrumentExtensions
{
    /// <summary>Gets the number of significant decimal digits in the instrument's tick size, for formatting prices.</summary>
    /// <typeparam name="TInstrument">The instrument type.</typeparam>
    /// <param name="instrument">The instrument to inspect.</param>
    /// <returns>The number of decimal digits in <see cref="IInstrument.TickSize"/>.</returns>
    public static int TickPrecision<TInstrument>(this TInstrument instrument)
        where TInstrument : IInstrument
    {
        return instrument.TickSize.Align().Decimals();
    }

    /// <summary>Rounds a quantity down to the nearest lot size, then clamps it into the instrument's allowed quantity range.</summary>
    /// <typeparam name="TInstrument">The instrument type.</typeparam>
    /// <param name="instrument">The instrument whose lot size and quantity bounds apply.</param>
    /// <param name="qty">The quantity to normalize, in the instrument's base asset.</param>
    /// <returns>The quantity aligned to <see cref="IInstrument.LotSize"/> and clamped between <see cref="IInstrument.MinQty"/> and <see cref="IInstrument.MaxQty"/>.</returns>
    public static decimal ToValidQty<TInstrument>(this TInstrument instrument, decimal qty)
        where TInstrument : IInstrument
    {
        var q = instrument.ToLotSize(qty);

        // the side comes from what was asked for, not from what survived alignment. A sell smaller than one
        // lot aligns to zero, and zero reads as positive - so taking the branch from the aligned value
        // turned a sell into a buy of the minimum quantity, which is an order on the wrong side of the book
        return qty >= 0
            ? Math.Min(Math.Max(q, instrument.MinQty), instrument.MaxQty)
            : -Math.Min(Math.Max(-q, instrument.MinQty), instrument.MaxQty);
    }

    /// <summary>Determines whether a quantity and price already satisfy the instrument's lot size, tick size and notional-value constraints.</summary>
    /// <typeparam name="TInstrument">The instrument type.</typeparam>
    /// <param name="instrument">The instrument whose constraints apply.</param>
    /// <param name="qty">The order quantity to check, in the instrument's base asset.</param>
    /// <param name="price">The order price to check.</param>
    /// <returns>True if the quantity is already lot-aligned and bounded, the price is already tick-aligned and within <see cref="IInstrument.MinPrice"/> and <see cref="IInstrument.MaxPrice"/>, and the resulting notional value is within <see cref="IInstrument.MinSum"/> and <see cref="IInstrument.MaxSum"/>; false otherwise.</returns>
    public static bool IsValidQtyPrice<TInstrument>(this TInstrument instrument, decimal qty, decimal price)
        where TInstrument : IInstrument
    {
        if (qty != instrument.ToValidQty(qty) || price != instrument.ToTickSizeRound(price))
            return false;

        // the price bounds are loaded from the exchange like every other limit here; checking only the
        // tick alignment let a price the exchange will reject pass as valid, and the rejection then
        // arrived from the exchange with nothing on this side explaining which constraint was missed.
        // Zero means the exchange does not enforce that bound - Binance's price filter says so in the
        // same way its lot and tick sizes do, which is why those are guarded on `> 0` too
        if (instrument.MinPrice > 0 && price < instrument.MinPrice)
            return false;

        if (instrument.MaxPrice > 0 && price > instrument.MaxPrice)
            return false;

        var sum = qty * price;
        return sum >= instrument.MinSum && sum <= instrument.MaxSum;
    }

    /// <summary>Rounds a price down to the nearest tick size.</summary>
    /// <typeparam name="TInstrument">The instrument type.</typeparam>
    /// <param name="instrument">The instrument whose tick size applies.</param>
    /// <param name="price">The price to round.</param>
    /// <returns>The price rounded down to the nearest multiple of <see cref="IInstrument.TickSize"/>, or the price unchanged if the tick size is zero.</returns>
    public static decimal ToTickSizeDown<TInstrument>(this TInstrument instrument, decimal price)
        where TInstrument : IInstrument
    {
        var tick = instrument.TickSize;
        return tick > 0 ? Math.Floor(price / tick) * tick : price;
    }

    /// <summary>Rounds a price to the nearest tick size.</summary>
    /// <typeparam name="TInstrument">The instrument type.</typeparam>
    /// <param name="instrument">The instrument whose tick size applies.</param>
    /// <param name="price">The price to round.</param>
    /// <returns>The price rounded to the nearest multiple of <see cref="IInstrument.TickSize"/>, or the price unchanged if the tick size is zero.</returns>
    public static decimal ToTickSizeRound<TInstrument>(this TInstrument instrument, decimal price)
        where TInstrument : IInstrument
    {
        var tick = instrument.TickSize;
        return tick > 0 ? Math.Round(price / tick) * tick : price;
    }

    /// <summary>Rounds a price up to the nearest tick size.</summary>
    /// <typeparam name="TInstrument">The instrument type.</typeparam>
    /// <param name="instrument">The instrument whose tick size applies.</param>
    /// <param name="price">The price to round.</param>
    /// <returns>The price rounded up to the nearest multiple of <see cref="IInstrument.TickSize"/>, or the price unchanged if the tick size is zero.</returns>
    public static decimal ToTickSizeUp<TInstrument>(this TInstrument instrument, decimal price)
        where TInstrument : IInstrument
    {
        var tick = instrument.TickSize;
        return tick > 0 ? Math.Ceiling(price / tick) * tick : price;
    }

    /// <summary>Rounds a quantity towards zero to the nearest lot size.</summary>
    /// <typeparam name="TInstrument">The instrument type.</typeparam>
    /// <param name="instrument">The instrument whose lot size applies.</param>
    /// <param name="qty">The quantity to round, in the instrument's base asset. May be negative, to express a sell/short size.</param>
    /// <returns>The quantity rounded towards zero to the nearest multiple of <see cref="IInstrument.LotSize"/>, or the quantity unchanged if the lot size is zero.</returns>
    public static decimal ToLotSize<TInstrument>(this TInstrument instrument, decimal qty)
        where TInstrument : IInstrument
    {
        // towards zero, not down: a negative quantity is a sell size, and rounding it down grows it.
        // Asking to close 0.13 of a position would have sent an order for 0.2 - more than was asked
        // for, on the side that reduces an exposure, which is the direction that must never overshoot
        var lot = instrument.LotSize;
        return lot > 0 ? Math.Truncate(qty / lot) * lot : qty;
    }
}
