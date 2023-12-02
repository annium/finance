using Annium.Finance.Providers.Abstractions.Domain.Enums;
using Annium.Finance.Providers.Abstractions.Domain.Interfaces;

namespace Annium.Finance.Providers.Abstractions.Domain.Dto;

public sealed record PositionDto(
    string Symbol,
    OrientationRange OrientationRange,
    MarginType MarginType,
    byte Leverage,
    decimal Amount
) : IPositionBase;
