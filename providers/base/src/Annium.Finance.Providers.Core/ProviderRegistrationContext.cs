using System;
using Annium.Core.DependencyInjection;
using Annium.Finance.Providers.Abstractions.Connectors.Market;
using Annium.Finance.Providers.Abstractions.Connectors.User;
using Annium.Finance.Providers.Abstractions.Domain.Shared;
using Annium.Finance.Providers.Abstractions.Domain.Temp;
using Annium.Finance.Providers.Core.Internal.Shared.TimeSync;
using Annium.Finance.Providers.Core.Shared.Status;
using Annium.Finance.Providers.Core.Shared.TimeSync;
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
    >(ProviderBaseConfiguration cfg)
        where TMarketProvider : IMarketProvider
        where TMarketConnector : IMarketConnector
        where TUserProvider : IUserProvider
        where TUserConnector : IUserConnector
        where TFinanceService : IFinanceService
    {
        var (provider, environments, serverTimeConfig) = cfg;

        Container.Add<TMarketProvider>().AsKeyed<IMarketProvider>(provider).AsSelf().Singleton();
        Container.Add<TMarketConnector>().AsSelf().Transient();
        Container.Add<TUserProvider>().AsKeyed<IUserProvider>(provider).AsSelf().Singleton();
        Container.Add<TUserConnector>().AsSelf().Transient();
        Container.Add<TFinanceService>().AsSelf().Transient();

        foreach (var env in environments.EnumerateFlags())
        {
            var providerKey = ProviderKey.Create(provider, env);
            Container.Add(providerKey).AsSelf().Singleton();
            Container.Add(serverTimeConfig).AsKeyed<ServerTimeProviderConfig>(providerKey).Singleton();
            Container
                .Add<Func<IServiceProvider, TMarketConnector>>(sp => sp.Resolve<TMarketConnector>())
                .AsKeyed<Func<IServiceProvider, IMarketConnector>>(providerKey)
                .Singleton();
            Container
                .Add<Func<IServiceProvider, TUserConnector>>(sp => sp.Resolve<TUserConnector>())
                .AsKeyed<Func<IServiceProvider, IUserConnector>>(providerKey)
                .Singleton();
            Container.Add<TFinanceService>().AsKeyed<IFinanceService>(providerKey).Singleton();
            Container.Add(ServerTimeSourceFactory).AsKeyed<IServerTimeSource>(providerKey).Scoped();
        }

        return this;
    }

    private static IServerTimeSource ServerTimeSourceFactory(IServiceProvider sp, object key)
    {
        var provider = sp.ResolveKeyed<IServerTimeProvider>(key);
        var config = sp.ResolveKeyed<ServerTimeProviderConfig>(key);
        var reporter = sp.Resolve<IStatusReporter>();
        var logger = sp.Resolve<ILogger>();

        return new ServerTimeSource(provider, config, reporter, logger);
    }
}
