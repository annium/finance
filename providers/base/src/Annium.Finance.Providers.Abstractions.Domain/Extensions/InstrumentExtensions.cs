using System;
using Annium.Finance.Providers.Abstractions.Domain.Interfaces;

namespace Annium.Finance.Providers.Abstractions.Domain.Extensions;

public static class InstrumentExtensions
{
    public static int TickPrecision<TInstrument>(this TInstrument instrument)
        where TInstrument : IInstrumentBase
    {
        return instrument.TickSize.Align().Decimals();
    }

    public static decimal ToValidQty<TInstrument>(this TInstrument instrument, decimal qty)
        where TInstrument : IInstrumentBase
    {
        var q = instrument.ToLotSize(qty);

        return q >= 0
            ? Math.Min(Math.Max(q, instrument.MinQty), instrument.MaxQty)
            : -Math.Min(Math.Max(-q, instrument.MinQty), instrument.MaxQty);
    }

    public static bool IsValidQtyPrice<TInstrument>(this TInstrument instrument, decimal qty, decimal price)
        where TInstrument : IInstrumentBase
    {
        if (qty != instrument.ToValidQty(qty) || price != instrument.ToTickSizeRound(price))
            return false;

        return qty * price >= instrument.MinSum;
    }

    public static decimal ToTickSizeDown<TInstrument>(this TInstrument instrument, decimal price)
        where TInstrument : IInstrumentBase
    {
        var tick = instrument.TickSize;
        return tick > 0 ? Math.Floor(price / tick) * tick : price;
    }

    public static decimal ToTickSizeRound<TInstrument>(this TInstrument instrument, decimal price)
        where TInstrument : IInstrumentBase
    {
        var tick = instrument.TickSize;
        return tick > 0 ? Math.Round(price / tick) * tick : price;
    }

    public static decimal ToTickSizeUp<TInstrument>(this TInstrument instrument, decimal price)
        where TInstrument : IInstrumentBase
    {
        var tick = instrument.TickSize;
        return tick > 0 ? Math.Ceiling(price / tick) * tick : price;
    }

    public static decimal ToLotSize<TInstrument>(this TInstrument instrument, decimal qty)
        where TInstrument : IInstrumentBase
    {
        var lot = instrument.LotSize;
        return lot > 0 ? Math.Floor(qty / lot) * lot : qty;
    }
}
