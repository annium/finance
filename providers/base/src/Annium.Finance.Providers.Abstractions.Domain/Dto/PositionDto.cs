using System;
using Annium.Finance.Providers.Abstractions.Domain.Enums;

namespace Annium.Finance.Providers.Abstractions.Domain.Dto;

public sealed record PositionDto(string Symbol, OrientationRange OrientationRange, MarginType MarginType, byte Leverage)
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public PositionDto SetId(Guid id)
    {
        Id = id;

        return this;
    }
}
