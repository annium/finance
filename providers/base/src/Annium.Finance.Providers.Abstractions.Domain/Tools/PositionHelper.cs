using System;
using System.Runtime.CompilerServices;
using Annium.Finance.Providers.Abstractions.Domain.Enums;
using OneOf;
using static Annium.Finance.Providers.Abstractions.Domain.Enums.PositionState;

namespace Annium.Finance.Providers.Abstractions.Domain.Tools;

public static class PositionHelper
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OneOf<PositionState, Exception> ResolveState(
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
            return new InvalidOperationException(
                $"Too much opens: openingQty {openingQty} + openedQty {openedQty} > TotalQty {totalQty}."
            );

        if (closingQty + closedQty > openingQty + openedQty)
            return new InvalidOperationException(
                $"Too much closes: closingQty {closingQty} + closedQty {closedQty} > openingQty {openingQty} + openedQty {openedQty}."
            );

        // total and others are 0
        if (totalQty == 0m)
            return Blank;

        // total > 0

        var state = default(PositionState);

        if (openingQty > 0)
            state |= Opening;

        if (openedQty > 0)
            state |= Opened;

        if (closingQty > 0)
            state |= Closing;

        if (closedQty > 0)
            state |= Closed;

        if (state == default)
            return Canceled;

        return state == (Opened | Closed) && openedQty == closedQty ? Filled : state;
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
