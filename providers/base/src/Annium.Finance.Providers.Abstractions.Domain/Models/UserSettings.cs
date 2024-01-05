using Annium.Finance.Providers.Abstractions.Domain.Enums;
using Annium.Finance.Providers.Abstractions.Domain.Interfaces;

namespace Annium.Finance.Providers.Abstractions.Domain.Models;

public sealed record UserSettings : IConnectorSettings
{
    public string Provider { get; init; } = string.Empty;
    public ProviderEnvironment Environment { get; init; }
    public string Key { get; init; } = string.Empty;
    public string Secret { get; init; } = string.Empty;

    public override string ToString() => $"{Provider}[{Environment}] {{{Key}}}";
}
