using System.Runtime.CompilerServices;
using Annium.Data.Operations;
using static Annium.Finance.Providers.Abstractions.Domain.User.PositionState;

namespace Annium.Finance.Providers.Abstractions.Domain.User.Helpers;

/// <summary>
/// Derives a position's lifecycle state and average price from the running quantities its opens and closes have accumulated.
/// </summary>
public static class PositionHelper
{
    /// <summary>Derives the lifecycle state of a position from its opening, opened, closing and closed quantities.</summary>
    /// <typeparam name="T">The type of the subject the quantities belong to, used only for error messages.</typeparam>
    /// <param name="subject">The subject (order or position) the quantities belong to, used to identify errors.</param>
    /// <param name="totalQty">The total quantity the position is sized for.</param>
    /// <param name="openingQty">The quantity currently being opened by unfilled or partially filled opening orders.</param>
    /// <param name="openedQty">The quantity already opened by filled opening orders.</param>
    /// <param name="closingQty">The quantity currently being closed by unfilled or partially filled closing orders.</param>
    /// <param name="closedQty">The quantity already closed by filled closing orders.</param>
    /// <returns>A result carrying the resolved <see cref="PositionState"/>, or errors if the quantities are inconsistent.</returns>
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
        var result = Result.Create(Blank);
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
            return Result.Create(Canceled);

        var resultState = state == (Opened | Closed) && openedQty == closedQty ? Filled : state;

        return Result.Create(resultState);
    }

    /// <summary>Derives the volume-weighted average price of a position after blending in a newly executed fill.</summary>
    /// <typeparam name="T">The type of the subject the quantities belong to, used only for error messages.</typeparam>
    /// <param name="subject">The subject (order or position) the quantities belong to, used to identify errors.</param>
    /// <param name="currentQty">The quantity already held before the new fill.</param>
    /// <param name="currentPrice">The average price already held before the new fill.</param>
    /// <param name="executedQty">The quantity of the new fill.</param>
    /// <param name="executedPrice">The price of the new fill.</param>
    /// <returns>A result carrying the new volume-weighted average price, or errors if any quantity or price is negative.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IResult<decimal> ResolvePrice<T>(
        T subject,
        decimal currentQty,
        decimal currentPrice,
        decimal executedQty,
        decimal executedPrice
    )
    {
        var result = Result.Create(0m);
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

        return Result.Create(price);
    }

    /// <summary>Adds an error to the result if the given value is negative.</summary>
    /// <typeparam name="TS">The type of the subject the value belongs to, used only for error messages.</typeparam>
    /// <typeparam name="TR">The type of data carried by the result to add the error to.</typeparam>
    /// <param name="subject">The subject the value belongs to, used to identify the error.</param>
    /// <param name="value">The value to check.</param>
    /// <param name="result">The result to add an error to if the check fails.</param>
    /// <param name="ex">The source expression of <paramref name="value"/>, captured automatically for the error message.</param>
    private static void CheckNonNegative<TS, TR>(
        TS subject,
        decimal value,
        IResult<TR> result,
        [CallerArgumentExpression(nameof(value))] string ex = ""
    )
    {
        if (value < 0)
            result.Error($"{subject} {ex} must be >=0");
    }
}
