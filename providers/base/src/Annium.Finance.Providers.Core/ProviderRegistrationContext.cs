using System;
using System.Linq;
using Annium.Core.DependencyInjection;
using Annium.Finance.Providers.Abstractions.Connectors.Market;
using Annium.Finance.Providers.Abstractions.Connectors.User;
using Annium.Finance.Providers.Abstractions.Domain.Market;
using Annium.Finance.Providers.Abstractions.Domain.Shared;
using Annium.Finance.Providers.Core.Internal.Shared.ServerTime;
using Annium.Finance.Providers.Core.Shared.ServerTime;
using Annium.Finance.Providers.Core.Shared.Status;
using Annium.Logging;

namespace Annium.Finance.Providers.Core;

public readonly struct ProviderRegistrationContext
{
    public readonly IServiceContainer Container;
    public readonly ServiceLifetime Lifetime;

    public ProviderRegistrationContext(IServiceContainer container, ServiceLifetime lifetime)
    {
        Container = container;
        Lifetime = lifetime;
    }

    public ProviderRegistrationContext AddProvider<
        TMarketProvider,
        TMarketConnector,
        TUserProvider,
        TUserConnector,
        TFinanceService
    >(
        Func<IServiceProvider, object, IServerTimeProvider> serverTimeProviderFactory,
        string provider,
        ProviderEnvironment environment
    )
        where TMarketProvider : IMarketProvider
        where TMarketConnector : IMarketConnector
        where TUserProvider : IUserProvider
        where TUserConnector : IUserConnector
        where TFinanceService : IFinanceService
    {
        Container.Add<TMarketProvider>().AsKeyed<IMarketProvider>(provider).AsSelf().Singleton();
        Container.Add<TMarketConnector>().AsSelf().Transient();
        Container.Add<TUserProvider>().AsKeyed<IUserProvider>(provider).AsSelf().Singleton();
        Container.Add<TUserConnector>().AsSelf().Transient();
        Container.Add<TFinanceService>().AsSelf().Transient();

        foreach (var env in Enum.GetValues<ProviderEnvironment>().Where(x => environment.HasFlag(x)))
        {
            var providerKey = ProviderKey.Create(provider, env);
            Container.Add(providerKey).AsSelf().Singleton();
            Container
                .Add<Func<IServiceProvider, TMarketConnector>>(sp => sp.Resolve<TMarketConnector>())
                .AsKeyed<Func<IServiceProvider, IMarketConnector>>(providerKey)
                .Singleton();
            Container
                .Add<Func<IServiceProvider, TUserConnector>>(sp => sp.Resolve<TUserConnector>())
                .AsKeyed<Func<IServiceProvider, IUserConnector>>(providerKey)
                .Singleton();
            Container.Add<TFinanceService>().AsKeyed<IFinanceService>(providerKey).Singleton();
            Container.Add(serverTimeProviderFactory).AsKeyed<IServerTimeProvider>(providerKey).Singleton();
            Container.Add(ServerTimeTrackerFactory).As<IServerTimeTracker>().Scoped();
        }

        return this;
    }

    private static IServerTimeTracker ServerTimeTrackerFactory(IServiceProvider sp)
    {
        var settings = sp.Resolve<Injected<MarketSettings>>().Value;
        var key = settings.GetProviderKey();

        var provider = sp.ResolveKeyed<IServerTimeProvider>(key);
        var reporter = sp.Resolve<IStatusReporter>();
        var logger = sp.Resolve<ILogger>();

        return new ServerTimeTracker(provider, reporter, logger);
    }
}
