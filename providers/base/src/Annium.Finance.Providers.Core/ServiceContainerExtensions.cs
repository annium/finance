using Annium.Core.DependencyInjection;
using Annium.Extensions.Pooling;
using Annium.Finance.Providers.Abstractions.Connectors.Market;
using Annium.Finance.Providers.Abstractions.Connectors.User;
using Annium.Finance.Providers.Abstractions.Domain.Market;
using Annium.Finance.Providers.Abstractions.Domain.Shared;
using Annium.Finance.Providers.Abstractions.Domain.User;
using Annium.Finance.Providers.Core.Internal.Connectors;
using Annium.Finance.Providers.Core.Internal.Shared.Loaders;
using Annium.Finance.Providers.Core.Internal.Shared.RateLimits;
using Annium.Finance.Providers.Core.Internal.Shared.Status;
using Annium.Finance.Providers.Core.Shared.Loaders;
using Annium.Finance.Providers.Core.Shared.RateLimits;
using Annium.Finance.Providers.Core.Shared.Status;

namespace Annium.Finance.Providers.Core;

public static class ServiceContainerExtensions
{
    public static ProviderRegistrationContext AddFinanceProviders(
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
        container.Add<IRateLimiterFactory, RateLimiterFactory>().Singleton();

        var ctx = new ProviderRegistrationContext(container, lifetime);

        return ctx;
    }
}
