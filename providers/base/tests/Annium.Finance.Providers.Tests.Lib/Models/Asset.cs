using System;
using Annium.Finance.Providers.Abstractions.Domain.Interfaces;

namespace Annium.Finance.Providers.Tests.Lib.Models;

public sealed record Asset(Guid Id, Guid ResourceId, Resource Resource, decimal Free, decimal Locked) : IAsset<Resource>
{
    public decimal Free { get; private set; } = Free;
    public decimal Locked { get; private set; } = Locked;

    public Asset Update(decimal free, decimal locked)
    {
        Free = free;
        Locked = locked;

        return this;
    }
}
