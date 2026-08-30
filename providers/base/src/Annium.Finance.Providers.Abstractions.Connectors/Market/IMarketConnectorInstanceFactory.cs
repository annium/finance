using Annium.Finance.Providers.Abstractions.Domain.Market;

namespace Annium.Finance.Providers.Abstractions.Connectors.Market;

/// <summary>
/// Creates <see cref="IMarketConnector"/> instances whose lifetime is tied to a caller-supplied disposable box,
/// letting the caller create and dispose ad-hoc connectors outside of the standard DI lifetime.
/// </summary>
public interface IMarketConnectorInstanceFactory
{
    /// <summary>
    /// Creates a market connector configured with the given settings, bound to the given disposable box.
    /// </summary>
    /// <param name="settings">The market settings identifying the provider and market to connect to.</param>
    /// <param name="disposable">The disposable box the connector registers its resources with; disposing it tears the connector down.</param>
    /// <returns>A new market connector instance.</returns>
    IMarketConnector Create(MarketSettings settings, AsyncDisposableBox disposable);
}
