using System;
using Annium.Core.DependencyInjection;
using Annium.Finance.Providers.Abstractions.Connectors.Market;
using Annium.Finance.Providers.Abstractions.Domain.Market;
using Annium.Finance.Providers.Core.Shared;
using Annium.Finance.Providers.Core.Shared.Loaders;

namespace Annium.Finance.Providers.Core.Market;

public static class ServiceProviderExtensions
{
    public static IMarketProvider CreateMarketProvider(this IServiceProvider sp, MarketSettings settings)
    {
        var providerFactory = sp.ResolveKeyed<IMarketProviderFactory>(settings.Provider);

        var provider = providerFactory.Create(settings);

        return provider;
    }

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
