using System;
using Annium.Core.DependencyInjection;
using Annium.Finance.Providers.Abstractions.Connectors.User;
using Annium.Finance.Providers.Abstractions.Domain.User;
using Annium.Finance.Providers.Core.Shared.Loaders;

namespace Annium.Finance.Providers.Core.User;

public static class ServiceProviderExtensions
{
    public static IUserProvider CreateUserProvider(this IServiceProvider sp, UserSettings settings)
    {
        var providerFactory = sp.ResolveKeyed<IUserProviderFactory>(settings.Provider);

        var provider = providerFactory.Create(settings);

        return provider;
    }

    public static ICompositeLoader<UserContext> CreateUserContextLoader(
        this IServiceProvider sp,
        CompositeLoaderConfig config,
        IUserProvider provider,
        ref AsyncDisposableBox disposable
    )
    {
        var loaderFactory = sp.Resolve<ILoaderFactory>();

        var loader = loaderFactory.CreateCompositeLoader<UserContext>(
            config,
            async _ => await provider.LoadContextAsync()
        );
        disposable += loader;

        return loader;
    }
}
