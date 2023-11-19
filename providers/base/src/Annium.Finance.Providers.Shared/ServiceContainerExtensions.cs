using Annium.Finance.Providers.Abstractions.Connectors.Connectors;
using Annium.Finance.Providers.Abstractions.Connectors.Services;
using Annium.Finance.Providers.Abstractions.Domain.Interfaces;
using Annium.Finance.Providers.Abstractions.Domain.Models;
using Annium.Finance.Providers.Shared;
using Annium.Finance.Providers.Shared.Internal.Connectors;
using Annium.Finance.Providers.Shared.Internal.Services;
using Annium.Finance.Providers.Shared.Internal.Sync;
using Annium.Finance.Providers.Shared.Services;

// ReSharper disable once CheckNamespace
namespace Annium.Core.DependencyInjection;

public static class ServiceContainerExtensions
{
    public static ProviderRegistrationContext AddProviders(this IServiceContainer container)
    {
        // market
        container.AddObjectCache<
            IMarketConfig,
            IMarketConnector,
            ConnectorCacheProvider<IMarketConfig, IMarketConnector>
        >(ServiceLifetime.Singleton);
        container.Add<Injected<IMarketConfig>>().AsSelf().Scoped();

        // user
        container.AddObjectCache<IUserConfig, IUserConnector, ConnectorCacheProvider<IUserConfig, IUserConnector>>(
            ServiceLifetime.Singleton
        );
        container.Add<Injected<IUserConfig>>().AsSelf().Scoped();

        // status
        container.Add<StatusMonitor>().AsSelf().AsInterfaces().Scoped();
        container.Add<StatusReporter>().AsSelf().AsInterfaces().Transient();

        // services
        container.AddObjectCache<ProviderKey, IFinanceService, FinanceServiceCacheProvider>(ServiceLifetime.Singleton);
        container.Add<ILoaderFactory, LoaderFactory>().Scoped();

        // common
        container.AddScheduler();
        container.AddTables();

        var ctx = new ProviderRegistrationContext(container);

        ctx.WithMarketSynchronizer<NoopMarketSynchronizer>();
        ctx.WithUserSynchronizer<NoopUserSynchronizer>();

        return ctx;
    }
}
