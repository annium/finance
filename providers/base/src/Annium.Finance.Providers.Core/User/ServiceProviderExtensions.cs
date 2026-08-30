using System;
using Annium.Core.DependencyInjection;
using Annium.Finance.Providers.Abstractions.Connectors.User;
using Annium.Finance.Providers.Abstractions.Domain.User;
using Annium.Finance.Providers.Core.Shared;
using Annium.Finance.Providers.Core.Shared.Loaders;

namespace Annium.Finance.Providers.Core.User;

/// <summary>
/// Factory extension methods for creating user providers and their context loaders.
/// </summary>
public static class ServiceProviderExtensions
{
    /// <summary>
    /// Creates a user provider for the given settings, resolving the provider-specific factory registered for
    /// the settings' provider key.
    /// </summary>
    /// <param name="sp">The service provider to resolve the provider-specific factory from.</param>
    /// <param name="settings">The user settings identifying the provider and account to connect to.</param>
    /// <returns>A new user provider instance.</returns>
    public static IUserProvider CreateUserProvider(this IServiceProvider sp, UserSettings settings)
    {
        var providerFactory = sp.ResolveKeyed<IUserProviderFactory>(settings.Provider);

        var provider = providerFactory.Create(settings);

        return provider;
    }

    /// <summary>
    /// Creates a composite loader that loads a <see cref="UserContext"/> via <paramref name="provider"/>, and
    /// registers it for disposal in <paramref name="disposable"/>.
    /// </summary>
    /// <param name="sp">The service provider to resolve dependencies from.</param>
    /// <param name="config">The timing configuration for fetch retries, interval reloads, and debounced requests.</param>
    /// <param name="provider">The user provider to load the context from.</param>
    /// <param name="disposable">The disposable box the loader is added to.</param>
    /// <returns>A new composite loader for the user context.</returns>
    public static ICompositeLoader<UserContext> CreateUserContextLoader(
        this IServiceProvider sp,
        CompositeLoaderConfig config,
        IUserProvider provider,
        ref AsyncDisposableBox disposable
    )
    {
        var loader = sp.CreateCompositeLoader<UserContext>(config, async _ => await provider.LoadContextAsync());
        disposable += loader;

        return loader;
    }
}
