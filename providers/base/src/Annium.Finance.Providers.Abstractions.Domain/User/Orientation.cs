using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Annium.Finance.Providers.Abstractions.Domain.User;

public sealed class Orientation
{
    private static readonly IDictionary<OrientationType, Orientation> _values =
        new Dictionary<OrientationType, Orientation>();
    public static Orientation Long { get; } = new(OrientationType.Long, OrderSide.Buy, OrderSide.Sell);
    public static Orientation Short { get; } = new(OrientationType.Short, OrderSide.Sell, OrderSide.Buy);

    public OrientationType Type { get; }
    public OrderSide OpenSide { get; }
    public OrderSide CloseSide { get; }

    private Orientation(OrientationType type, OrderSide openSide, OrderSide closeSide)
    {
        Type = type;
        OpenSide = openSide;
        CloseSide = closeSide;
        _values[Type] = this;
    }

    public override string ToString() => $"{Type} ({OpenSide} -> {CloseSide})";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Orientation(OrientationType type) => _values[type];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator OrientationType(Orientation orientation) => orientation.Type;
}
