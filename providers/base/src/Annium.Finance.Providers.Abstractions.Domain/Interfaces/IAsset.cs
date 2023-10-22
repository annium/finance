using System;

namespace Annium.Finance.Providers.Abstractions.Domain.Interfaces;

public interface IAsset<TResource> : IAsset
    where TResource : IResource
{
    Guid ResourceId { get; }
    TResource Resource { get; }
}

public interface IAsset
{
    Guid Id { get; }
    decimal Free { get; }
    decimal Locked { get; }
}