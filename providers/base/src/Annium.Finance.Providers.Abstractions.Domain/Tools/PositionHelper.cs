using System;
using System.Runtime.CompilerServices;
using Annium.Finance.Providers.Abstractions.Domain.Enums;

namespace Annium.Finance.Providers.Abstractions.Domain.Tools;

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
        EnsureNonNegative(totalQty);
        EnsureNonNegative(openingQty);
        EnsureNonNegative(openedQty);
        EnsureNonNegative(closingQty);
        EnsureNonNegative(closedQty);

        if (openingQty + openedQty > totalQty)
            throw new InvalidOperationException(
                $"Too much opens: openingQty {openingQty} + openedQty {openedQty} > TotalQty {totalQty}."
            );

        if (closingQty + closedQty > openingQty + openedQty)
            throw new InvalidOperationException(
                $"Too much closes: closingQty {closingQty} + closedQty {closedQty} > openingQty {openingQty} + openedQty {openedQty}."
            );

        // total and others are 0
        if (totalQty == 0m)
            return PositionState.Blank;

        // total > 0

        if (openingQty > 0)
            return closingQty > 0 ? PositionState.Opening | PositionState.Closing : PositionState.Opening;

        if (closingQty > 0)
            return PositionState.Closing;

        // opening == 0, closing == 0

        // opened == 0, closed == 0
        if (openedQty == 0)
            return PositionState.Canceled;

        // opened >= 0, closed >= opened
        return openedQty > closedQty ? PositionState.Opened : PositionState.Closed;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static decimal ResolvePrice(
        decimal currentQty,
        decimal currentPrice,
        decimal executedQty,
        decimal executedPrice
    )
    {
        EnsureNonNegative(currentQty);
        EnsureNonNegative(currentPrice);
        EnsureNonNegative(executedQty);
        EnsureNonNegative(executedPrice);

        var totalQty = currentQty + executedQty;
        if (totalQty == 0)
            return 0;

        return (currentQty * currentPrice + executedQty * executedPrice) / totalQty;
    }

    private static void EnsureNonNegative(decimal value, [CallerArgumentExpression("value")] string ex = "")
    {
        if (value < 0)
            throw new ArgumentOutOfRangeException($"{ex} must be >=0");
    }
}
