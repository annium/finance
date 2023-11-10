using Annium.Finance.Providers.Abstractions.Connectors.Connectors;
using Annium.Finance.Providers.Abstractions.Connectors.Services;
using Annium.Finance.Providers.Abstractions.Connectors.Sync;
using Annium.Finance.Providers.Abstractions.Domain.Interfaces;
using Annium.Finance.Providers.Abstractions.Domain.Models;
using Annium.Finance.Providers.Shared;
using Annium.Finance.Providers.Shared.Connectors;
using Annium.Finance.Providers.Shared.Internal.Connectors;
using Annium.Finance.Providers.Shared.Internal.Services;
using Annium.Finance.Providers.Shared.Internal.Sync;

// ReSharper disable once CheckNamespace
namespace Annium.Core.DependencyInjection;

public static class ServiceContainerExtensions
{
    public static ProviderRegistrationContext AddProvidersSingleton(this IServiceContainer container) =>
        container.AddProviders(ServiceLifetime.Singleton);

    public static ProviderRegistrationContext AddProvidersScoped(this IServiceContainer container) =>
        container.AddProviders(ServiceLifetime.Scoped);

    private static ProviderRegistrationContext AddProviders(this IServiceContainer container, ServiceLifetime lifetime)
    {
        // market
        container.AddObjectCache<
            IMarketConfig,
            IMarketConnector,
            ConnectorCacheProvider<IMarketConfig, IMarketConnector>
        >(lifetime);
        container.Add<IMarketSynchronizer, NoopMarketSynchronizer>().In(lifetime);

        // user
        container.AddObjectCache<IUserConfig, IUserConnector, ConnectorCacheProvider<IUserConfig, IUserConnector>>(
            lifetime
        );
        container.Add<IUserSynchronizer, NoopUserSynchronizer>().In(lifetime);

        // services
        container.AddObjectCache<ProviderKey, IFinanceService, FinanceServiceCacheProvider>(lifetime);

        // common
        container.AddScheduler();
        container.AddTables();

        return new(container, lifetime);
    }
}
