using Annium.Finance.Providers.Abstractions.Domain.Enums;
using Annium.Finance.Providers.Abstractions.Domain.Interfaces;

namespace Annium.Finance.Providers.Abstractions.Domain.Models;

public sealed record PositionModel(
    string Symbol,
    OrientationRange OrientationRange,
    MarginType MarginType,
    decimal Leverage,
    decimal Amount
) : IPosition;
