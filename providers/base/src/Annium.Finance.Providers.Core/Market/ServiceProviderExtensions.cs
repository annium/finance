using System;
using Annium.Core.DependencyInjection;
using Annium.Finance.Providers.Abstractions.Connectors.Market;
using Annium.Finance.Providers.Abstractions.Domain.Market;
using Annium.Finance.Providers.Core.Shared;
using Annium.Finance.Providers.Core.Shared.Loaders;

namespace Annium.Finance.Providers.Core.Market;

/// <summary>
/// Factory extension methods for creating market providers and their context loaders.
/// </summary>
public static class ServiceProviderExtensions
{
    /// <summary>
    /// Creates a market provider for the given settings, resolving the provider-specific factory registered for
    /// the settings' provider key.
    /// </summary>
    /// <param name="sp">The service provider to resolve the provider-specific factory from.</param>
    /// <param name="settings">The market settings identifying the provider and market to connect to.</param>
    /// <returns>A new market provider instance.</returns>
    public static IMarketProvider CreateMarketProvider(this IServiceProvider sp, MarketSettings settings)
    {
        var providerFactory = sp.ResolveKeyed<IMarketProviderFactory>(settings.Provider);

        var provider = providerFactory.Create(settings);

        return provider;
    }

    /// <summary>
    /// Creates a composite loader that loads a <see cref="MarketContext"/> via <paramref name="provider"/>, and
    /// registers it for disposal in <paramref name="disposable"/>.
    /// </summary>
    /// <param name="sp">The service provider to resolve dependencies from.</param>
    /// <param name="config">The timing configuration for fetch retries, interval reloads, and debounced requests.</param>
    /// <param name="provider">The market provider to load the context from.</param>
    /// <param name="disposable">The disposable box the loader is added to.</param>
    /// <returns>A new composite loader for the market context.</returns>
    public static ICompositeLoader<MarketContext> CreateMarketContextLoader(
        this IServiceProvider sp,
        CompositeLoaderConfig config,
        IMarketProvider provider,
        ref AsyncDisposableBox disposable
    )
    {
        var loader = sp.CreateCompositeLoader<MarketContext>(config, async _ => await provider.LoadContextAsync());
        disposable += loader;

        return loader;
    }
}
