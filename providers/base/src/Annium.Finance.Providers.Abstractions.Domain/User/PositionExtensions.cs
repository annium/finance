using System.Runtime.CompilerServices;

namespace Annium.Finance.Providers.Abstractions.Domain.User;

/// <summary>
/// Provides margin composition helpers for <see cref="IPosition"/>.
/// </summary>
public static class PositionExtensions
{
    /// <summary>Gets the fraction of the position's notional value funded by the trader's own margin.</summary>
    /// <typeparam name="TPosition">The position type.</typeparam>
    /// <param name="position">The position to inspect.</param>
    /// <returns>The reciprocal of <see cref="IPosition.Leverage"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static decimal LeveragedPart<TPosition>(this TPosition position)
        where TPosition : IPosition => 1m / position.Leverage;

    /// <summary>Gets the fraction of the position's notional value funded by borrowed (leveraged) funds.</summary>
    /// <typeparam name="TPosition">The position type.</typeparam>
    /// <param name="position">The position to inspect.</param>
    /// <returns>One minus the reciprocal of <see cref="IPosition.Leverage"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static decimal BorrowedPart<TPosition>(this TPosition position)
        where TPosition : IPosition => 1m - 1m / position.Leverage;
}
