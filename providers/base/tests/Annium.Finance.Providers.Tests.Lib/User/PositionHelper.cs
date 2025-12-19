using System;
using System.Runtime.CompilerServices;
using Annium.Finance.Providers.Abstractions.Domain.User;
using Annium.Finance.Providers.Tests.Lib.Market;

namespace Annium.Finance.Providers.Tests.Lib.User;

public static class PositionHelper
{
    public static Position CreatePosition(decimal leverage) =>
        CreatePosition(InstrumentHelper.DefaultInstrument, leverage);

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
