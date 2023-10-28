using System.Runtime.CompilerServices;
using Annium.Finance.Providers.Abstractions.Domain.Interfaces;

namespace Annium.Finance.Providers.Abstractions.Domain.Extensions;

public static class PositionExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static decimal LeveragedPart<TPosition>(this TPosition position)
        where TPosition : IPosition => 1m / position.Leverage;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static decimal BorrowedPart<TPosition>(this TPosition position)
        where TPosition : IPosition => 1m - 1m / position.Leverage;
}
