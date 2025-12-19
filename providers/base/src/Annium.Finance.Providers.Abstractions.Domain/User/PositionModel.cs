namespace Annium.Finance.Providers.Abstractions.Domain.User;

public sealed record PositionModel(
    string Symbol,
    OrientationRange OrientationRange,
    MarginType MarginType,
    decimal Leverage,
    decimal Amount
) : IPosition;
