using Annium.Finance.Providers.Abstractions.Domain.Market;

namespace Annium.Finance.Providers.Abstractions.Connectors.Market;

/// <summary>
/// Creates <see cref="IMarketConnector"/> instances, resolving all their dependencies through the container
/// (used to build standalone connectors, e.g. registered as singletons in DI).
/// </summary>
public interface IMarketConnectorFactory
{
    /// <summary>
    /// Creates a market connector configured with the given settings.
    /// </summary>
    /// <param name="settings">The market settings identifying the provider and market to connect to.</param>
    /// <returns>A new market connector instance.</returns>
    IMarketConnector Create(MarketSettings settings);
}
