using System;
using Annium.Core.DependencyInjection;
using Annium.Finance.Providers.Abstractions.Connectors.Market;
using Annium.Finance.Providers.Abstractions.Connectors.User;
using Annium.Finance.Providers.Abstractions.Domain.Shared;
using Annium.Finance.Providers.Core.Internal.Shared.TimeSync;
using Annium.Finance.Providers.Core.Shared.Status;
using Annium.Finance.Providers.Core.Shared.TimeSync;
using Annium.Logging;

namespace Annium.Finance.Providers.Core;

/// <summary>
/// Fluent context returned by <see cref="ServiceContainerExtensions.AddFinanceProviders"/> for registering one
/// or more individual finance providers into the same container.
/// </summary>
public readonly struct ProviderRegistrationContext
{
    /// <summary>The container providers are registered into.</summary>
    public readonly IServiceContainer Container;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProviderRegistrationContext"/> struct.
    /// </summary>
    /// <param name="container">The container providers are registered into.</param>
    public ProviderRegistrationContext(IServiceContainer container)
    {
        Container = container;
    }

    /// <summary>
    /// Registers a single provider's market and user factories and its finance service, each keyed by
    /// <see cref="ProviderBaseConfiguration.Provider"/>, and a server time source per environment the provider
    /// is registered for, keyed by the environment-specific <see cref="ProviderKey"/>.
    /// </summary>
    /// <typeparam name="TMarketProviderFactory">The provider's market data access factory implementation.</typeparam>
    /// <typeparam name="TMarketConnectorFactory">The provider's market connector instance factory implementation.</typeparam>
    /// <typeparam name="TUserProviderFactory">The provider's user data access factory implementation.</typeparam>
    /// <typeparam name="TUserConnectorFactory">The provider's user connector instance factory implementation.</typeparam>
    /// <typeparam name="TFinanceService">The provider's leverage-aware arithmetic implementation.</typeparam>
    /// <param name="cfg">The provider's registration-time configuration.</param>
    /// <returns>This context, for chaining further registrations.</returns>
    public ProviderRegistrationContext AddProvider<
        TMarketProviderFactory,
        TMarketConnectorFactory,
        TUserProviderFactory,
        TUserConnectorFactory,
        TFinanceService
    >(ProviderBaseConfiguration cfg)
        where TMarketProviderFactory : IMarketProviderFactory
        where TMarketConnectorFactory : IMarketConnectorInstanceFactory
        where TUserProviderFactory : IUserProviderFactory
        where TUserConnectorFactory : IUserConnectorInstanceFactory
        where TFinanceService : IFinanceService
    {
        var (provider, environments, serverTimeConfig) = cfg;

        // market
        Container.Add<TMarketProviderFactory>().AsKeyed<IMarketProviderFactory>(provider).Scoped();
        Container.Add<TMarketConnectorFactory>().AsKeyed<IMarketConnectorInstanceFactory>(provider).Scoped();

        // user
        Container.Add<TUserProviderFactory>().AsKeyed<IUserProviderFactory>(provider).Scoped();
        Container.Add<TUserConnectorFactory>().AsKeyed<IUserConnectorInstanceFactory>(provider).Scoped();
        Container.Add<TFinanceService>().AsKeyed<IFinanceService>(provider).Scoped();

        foreach (var env in environments.EnumerateFlags())
        {
            var providerKey = ProviderKey.Create(provider, env);

            // shared
            Container.Add(providerKey).AsSelf().Singleton();
            Container.Add(serverTimeConfig).AsKeyed<ServerTimeProviderConfig>(providerKey).Singleton();
            Container.Add(ServerTimeSourceFactory).AsKeyed<IServerTimeSource>(providerKey).Scoped();
        }

        return this;
    }

    /// <summary>
    /// Resolves the keyed dependencies for a provider's environment and builds its server time source.
    /// </summary>
    /// <param name="sp">The service provider to resolve dependencies from.</param>
    /// <param name="key">The environment-specific <see cref="ProviderKey"/> the dependencies are keyed by.</param>
    /// <returns>A new server time source for the given provider/environment.</returns>
    private static IServerTimeSource ServerTimeSourceFactory(IServiceProvider sp, object key)
    {
        var provider = sp.ResolveKeyed<IServerTimeProvider>(key);
        var config = sp.ResolveKeyed<ServerTimeProviderConfig>(key);
        var reporter = sp.Resolve<IStatusReporter>();
        var logger = sp.Resolve<ILogger>();

        return new ServerTimeSource(provider, config, reporter, logger);
    }
}
