using System;
using Annium.Core.Mapper.Attributes;

namespace Annium.Finance.Providers.Abstractions.Domain.Shared;

/// <summary>
/// Identifies which environment a provider connection targets.
/// </summary>
[Flags]
[AutoMapped]
public enum ProviderEnvironment
{
    /// <summary>The live production environment, trading real funds.</summary>
    Real = 1 << 0,

    /// <summary>The provider's sandbox/testnet environment, trading no real funds.</summary>
    Test = 1 << 1,
}
