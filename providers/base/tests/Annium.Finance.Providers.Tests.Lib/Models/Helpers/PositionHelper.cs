using System;
using System.Runtime.CompilerServices;
using Annium.Finance.Providers.Abstractions.Domain.Enums;

namespace Annium.Finance.Providers.Tests.Lib.Models.Helpers;

public static class PositionHelper
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static PositionState ResolveState(
        decimal totalQty,
        decimal openingQty,
        decimal openedQty,
        decimal closingQty,
        decimal closedQty
    )
    {
        if (openingQty + openedQty > totalQty)
            throw new InvalidOperationException($"Too much opens: openingQty {openingQty} + openedQty {openedQty} > TotalQty {totalQty}.");

        if (closingQty + closedQty > openedQty)
            throw new InvalidOperationException($"Too much closes: closingQty {closingQty} + closedQty {closedQty} > openingQty {openingQty} + openedQty {openedQty}.");

        if (totalQty == 0m)
            return PositionState.Blank;

        if (openingQty > 0)
            return PositionState.Opening;

        if (closingQty > 0)
            return PositionState.Closing;

        if (openedQty == 0)
            return PositionState.Canceled;

        return openedQty > closedQty ? PositionState.Opened : PositionState.Closed;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static decimal ResolvePrice(decimal currentQty, decimal currentPrice, decimal executedQty, decimal executedPrice)
    {
        var totalQty = currentQty + executedQty;
        if (totalQty == 0)
            return 0;

        return (currentQty * currentPrice + executedQty * executedPrice) / totalQty;
    }
}