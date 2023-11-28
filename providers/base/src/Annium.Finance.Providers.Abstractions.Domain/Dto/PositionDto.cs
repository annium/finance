using Annium.Finance.Providers.Abstractions.Domain.Enums;

namespace Annium.Finance.Providers.Abstractions.Domain.Dto;

public sealed record PositionDto(
    string Symbol,
    OrientationRange OrientationRange,
    MarginType MarginType,
    byte Leverage
);
