namespace Annium.Finance.Providers.Abstractions.Domain.User;

/// <summary>
/// Represents a leveraged position held on an instrument, as reported by a provider.
/// </summary>
/// <param name="Symbol">The instrument symbol the position is held on.</param>
/// <param name="OrientationRange">The orientation (long, short, or both) the position covers.</param>
/// <param name="MarginType">How margin is allocated to the position (cross or isolated).</param>
/// <param name="Leverage">The leverage multiplier applied to the position.</param>
/// <param name="Amount">The signed size of the position, in the instrument's base asset (positive for long, negative for short).</param>
public sealed record PositionModel(
    string Symbol,
    OrientationRange OrientationRange,
    MarginType MarginType,
    decimal Leverage,
    decimal Amount
) : IPosition;
