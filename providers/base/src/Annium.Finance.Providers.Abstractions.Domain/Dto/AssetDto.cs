using System;

namespace Annium.Finance.Providers.Abstractions.Domain.Dto;

public sealed record AssetDto(string Resource, decimal Free, decimal Locked)
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public AssetDto SetId(Guid id)
    {
        Id = id;

        return this;
    }
}
