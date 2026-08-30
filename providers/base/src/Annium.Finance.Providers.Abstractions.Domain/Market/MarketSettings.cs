using Annium.Finance.Providers.Abstractions.Domain.Shared;

namespace Annium.Finance.Providers.Abstractions.Domain.Market;

/// <summary>
/// Configures the connection to a market data provider.
/// </summary>
public sealed record MarketSettings : IConnectorSettings
{
    /// <summary>Gets the name of the market data provider to connect to.</summary>
    public string Provider { get; init; } = string.Empty;

    /// <summary>Gets the environment (real or test) to connect to.</summary>
    public ProviderEnvironment Environment { get; init; }

    /// <summary>Returns the provider and environment as a string.</summary>
    /// <returns>A string in the form "Provider[Environment]".</returns>
    public override string ToString() => $"{Provider}[{Environment}]";
}
