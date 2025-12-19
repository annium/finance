using Annium.Finance.Providers.Abstractions.Domain.Shared;

namespace Annium.Finance.Providers.Abstractions.Domain.Market;

public sealed record MarketSettings : IConnectorSettings
{
    public string Provider { get; init; } = string.Empty;
    public ProviderEnvironment Environment { get; init; }

    public override string ToString() => $"{Provider}[{Environment}]";
}
