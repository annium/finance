using Annium.Extensions.Pooling;
using Annium.Finance.Providers.Abstractions.Connectors.Connectors;
using Annium.Finance.Providers.Abstractions.Connectors.Services;
using Annium.Finance.Providers.Abstractions.Domain.Models;
using Annium.Finance.Providers.Shared;
using Annium.Finance.Providers.Shared.Connectors;
using Annium.Finance.Providers.Shared.Internal.Connectors;
using Annium.Finance.Providers.Shared.Internal.Loaders;
using Annium.Finance.Providers.Shared.Internal.Services;
using Annium.Finance.Providers.Shared.Loaders;

// ReSharper disable once CheckNamespace
namespace Annium.Core.DependencyInjection;

public static class ServiceContainerExtensions
{
    public static ProviderRegistrationContext AddProviders(
        this IServiceContainer container,
        ServiceLifetime lifetime = ServiceLifetime.Singleton
    )
    {
        container.AddInjectables();

        // market
        container.AddObjectCache<MarketSettings, IMarketConnector, MarketConnectorCacheProvider>(lifetime);

        // user
        container.AddObjectCache<UserSettings, IUserConnector, UserConnectorCacheProvider>(lifetime);

        // status
        container.Add<StatusMonitor>().AsSelf().As<IStatusMonitor>().Scoped();
        container.Add<IStatusReporter, StatusReporter>().Transient();

        // loaders
        container.Add<ILoaderFactory, LoaderFactory>().Scoped();

        // services
        container.AddObjectCache<ProviderKey, IFinanceService, FinanceServiceCacheProvider>(lifetime);

        var ctx = new ProviderRegistrationContext(container, lifetime);

        return ctx;
    }
}
