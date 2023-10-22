using System;
using Annium.Finance.Providers.Abstractions.Domain.Enums;

namespace Annium.Finance.Providers.Abstractions.Domain.Interfaces;

public interface IResource
{
    Guid Id { get; }
    string Provider { get; }
    ProviderEnvironment Environment { get; }
    string Code { get; }
    byte Precision { get; }
}