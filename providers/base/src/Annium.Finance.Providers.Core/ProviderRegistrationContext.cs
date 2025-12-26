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

    public ProviderRegistrationContext(IServiceContainer container)
    {
        Container = container;
    }

    public ProviderRegistrationContext AddProvider<
        TMarketProviderFactory,
        TMarketConnectorFactory,
        TUserProviderFactory,
        TUserConnectorFactory,
        TFinanceService
    >(ProviderBaseConfiguration cfg)
        where TMarketProviderFactory : IMarketProviderFactory
        where TMarketConnectorFactory : IMarketConnectorFactory
        where TUserProviderFactory : IUserProviderFactory
        where TUserConnectorFactory : IUserConnectorFactory
        where TFinanceService : IFinanceService
    {
        var (provider, environments, serverTimeConfig) = cfg;

        // market
        Container.Add<TMarketProviderFactory>().AsKeyed<IMarketProviderFactory>(provider).Scoped();
        Container.Add<TMarketConnectorFactory>().AsKeyed<IMarketConnectorFactory>(provider).Scoped();

        // user
        Container.Add<TUserProviderFactory>().AsKeyed<IUserProviderFactory>(provider).Scoped();
        Container.Add<TUserConnectorFactory>().AsKeyed<IUserConnectorFactory>(provider).Scoped();
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

    private static IServerTimeSource ServerTimeSourceFactory(IServiceProvider sp, object key)
    {
        var provider = sp.ResolveKeyed<IServerTimeProvider>(key);
        var config = sp.ResolveKeyed<ServerTimeProviderConfig>(key);
        var reporter = sp.Resolve<IStatusReporter>();
        var logger = sp.Resolve<ILogger>();

        return new ServerTimeSource(provider, config, reporter, logger);
    }
}
