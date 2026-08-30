using Annium.Finance.Providers.Abstractions.Domain.Market;

namespace Annium.Finance.Providers.Abstractions.Connectors.Market;

/// <summary>
/// Creates <see cref="IMarketProvider"/> instances for a given market configuration.
/// </summary>
public interface IMarketProviderFactory
{
    /// <summary>
    /// Creates a market provider configured with the given settings.
    /// </summary>
    /// <param name="settings">The market settings identifying the provider and market to connect to.</param>
    /// <returns>A new market provider instance.</returns>
    IMarketProvider Create(MarketSettings settings);
}
