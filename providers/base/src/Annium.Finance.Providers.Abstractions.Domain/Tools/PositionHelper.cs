using System.Runtime.CompilerServices;
using Annium.Data.Operations;
using Annium.Finance.Providers.Abstractions.Domain.Enums;
using static Annium.Finance.Providers.Abstractions.Domain.Enums.PositionState;

namespace Annium.Finance.Providers.Abstractions.Domain.Tools;

public static class PositionHelper
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IResult<PositionState> ResolveState<T>(
        T subject,
        decimal totalQty,
        decimal openingQty,
        decimal openedQty,
        decimal closingQty,
        decimal closedQty
    )
    {
        var result = Result.New(Blank);
        CheckNonNegative(subject, totalQty, result);
        CheckNonNegative(subject, openingQty, result);
        CheckNonNegative(subject, openedQty, result);
        CheckNonNegative(subject, closingQty, result);
        CheckNonNegative(subject, closedQty, result);

        if (result.HasErrors)
            return result;

        if (openingQty + openedQty > totalQty)
        {
            result.Error(
                $"{subject} has too much opens: openingQty {openingQty} + openedQty {openedQty} > TotalQty {totalQty}."
            );
            return result;
        }

        if (closingQty + closedQty > openingQty + openedQty)
        {
            result.Error(
                $"{subject} has too much closes: closingQty {closingQty} + closedQty {closedQty} > openingQty {openingQty} + openedQty {openedQty}."
            );
            return result;
        }

        // total and others are 0 - return default blank value
        if (totalQty == 0m)
            return result;

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
            return Result.New(Canceled);

        var resultState = state == (Opened | Closed) && openedQty == closedQty ? Filled : state;

        return Result.New(resultState);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IResult<decimal> ResolvePrice<T>(
        T subject,
        decimal currentQty,
        decimal currentPrice,
        decimal executedQty,
        decimal executedPrice
    )
    {
        var result = Result.New(0m);
        CheckNonNegative(subject, currentQty, result);
        CheckNonNegative(subject, currentPrice, result);
        CheckNonNegative(subject, executedQty, result);
        CheckNonNegative(subject, executedPrice, result);

        if (result.HasErrors)
            return result;

        var totalQty = currentQty + executedQty;
        if (totalQty == 0)
            return result;

        var price = (currentQty * currentPrice + executedQty * executedPrice) / totalQty;

        return Result.New(price);
    }

    private static void CheckNonNegative<TS, TR>(
        TS subject,
        decimal value,
        IResult<TR> result,
        [CallerArgumentExpression("value")] string ex = ""
    )
    {
        if (value < 0)
            result.Error($"{subject} {ex} must be >=0");
    }
}
