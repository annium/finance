using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Annium.Finance.Providers.Abstractions.Domain.User;

/// <summary>
/// Maps a position's directional stance (long or short) to the order sides that open and close it.
/// </summary>
public sealed class Orientation
{
    /// <summary>Lookup of every known orientation by its <see cref="OrientationType"/>, populated as instances are constructed.</summary>
    private static readonly IDictionary<OrientationType, Orientation> _values =
        new Dictionary<OrientationType, Orientation>();

    /// <summary>Gets the long orientation: opened by buying, closed by selling.</summary>
    public static Orientation Long { get; } = new(OrientationType.Long, OrderSide.Buy, OrderSide.Sell);

    /// <summary>Gets the short orientation: opened by selling, closed by buying.</summary>
    public static Orientation Short { get; } = new(OrientationType.Short, OrderSide.Sell, OrderSide.Buy);

    /// <summary>Gets the directional stance this orientation represents.</summary>
    public OrientationType Type { get; }

    /// <summary>Gets the order side that opens or extends a position with this orientation.</summary>
    public OrderSide OpenSide { get; }

    /// <summary>Gets the order side that closes or reduces a position with this orientation.</summary>
    public OrderSide CloseSide { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Orientation"/> class.
    /// </summary>
    /// <param name="type">The directional stance this orientation represents.</param>
    /// <param name="openSide">The order side that opens or extends a position with this orientation.</param>
    /// <param name="closeSide">The order side that closes or reduces a position with this orientation.</param>
    private Orientation(OrientationType type, OrderSide openSide, OrderSide closeSide)
    {
        Type = type;
        OpenSide = openSide;
        CloseSide = closeSide;
        _values[Type] = this;
    }

    /// <summary>Returns the orientation's type and open/close sides as a string.</summary>
    /// <returns>A string in the form "Type (OpenSide -> CloseSide)".</returns>
    public override string ToString() => $"{Type} ({OpenSide} -> {CloseSide})";

    /// <summary>Converts an <see cref="OrientationType"/> into its corresponding <see cref="Orientation"/> instance.</summary>
    /// <param name="type">The directional stance to look up.</param>
    /// <returns>The <see cref="Orientation"/> matching the given type.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Orientation(OrientationType type) => _values[type];

    /// <summary>Converts an <see cref="Orientation"/> into its underlying <see cref="OrientationType"/>.</summary>
    /// <param name="orientation">The orientation to convert.</param>
    /// <returns>The value of <see cref="Type"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator OrientationType(Orientation orientation) => orientation.Type;
}
