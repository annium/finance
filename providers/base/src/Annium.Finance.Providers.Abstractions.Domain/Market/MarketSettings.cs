using Annium.Finance.Providers.Abstractions.Domain.Shared;

namespace Annium.Finance.Providers.Abstractions.Domain.Market;

/// <summary>
/// Configures the connection to a market data provider.
/// </summary>
public sealed record MarketSettings : IConnectorSettings
{
    /// <summary>Gets the name of the market data provider to connect to.</summary>
    public string Provider { get; init; } = string.Empty;

    /// <summary>Returns the provider name.</summary>
    /// <returns>The provider name.</returns>
    public override string ToString() => Provider;
}
