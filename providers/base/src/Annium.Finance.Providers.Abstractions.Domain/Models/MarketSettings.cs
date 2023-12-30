using Annium.Finance.Providers.Abstractions.Domain.Enums;
using Annium.Finance.Providers.Abstractions.Domain.Interfaces;

namespace Annium.Finance.Providers.Abstractions.Domain.Models;

public sealed record MarketSettings : IConnectorSettings
{
    public string Provider { get; init; } = string.Empty;
    public ProviderEnvironment Environment { get; init; }
}
