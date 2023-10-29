using System;
using System.Linq;
using Annium.Core.DependencyInjection;
using Annium.Finance.Providers.Abstractions.Connectors.Connectors;
using Annium.Finance.Providers.Abstractions.Connectors.Services;
using Annium.Finance.Providers.Abstractions.Domain.Enums;
using Annium.Finance.Providers.Abstractions.Domain.Models;

namespace Annium.Finance.Providers.Shared;

public readonly struct ProviderRegistrationContext
{
    public readonly IServiceContainer Container;
    private readonly ServiceLifetime _lifetime;

    public ProviderRegistrationContext(IServiceContainer container, ServiceLifetime lifetime)
    {
        Container = container;
        _lifetime = lifetime;
    }

    public ProviderRegistrationContext AddProvider<
        TMarketProvider,
        TMarketConnector,
        TUserProvider,
        TUserConnector,
        TFinanceService
    >(string provider, ProviderEnvironment environment)
        where TMarketProvider : IMarketProvider
        where TMarketConnector : IMarketConnector
        where TUserProvider : IUserProvider
        where TUserConnector : IUserConnector
        where TFinanceService : IFinanceService
    {
        Container.Add<TMarketProvider>().AsKeyed<IMarketProvider, string>(provider).AsSelf().In(_lifetime);
        Container.Add<TMarketConnector>().AsSelf().Transient();
        Container.Add<TUserProvider>().AsKeyed<IUserProvider, string>(provider).AsSelf().In(_lifetime);
        Container.Add<TUserConnector>().AsSelf().Transient();
        Container.Add<TFinanceService>().AsSelf().Transient();

        foreach (var env in Enum.GetValues<ProviderEnvironment>().Where(x => environment.HasFlag(x)))
        {
            var providerKey = ProviderKey.Create(provider, env);
            Container.Add(providerKey).AsSelf().Singleton();
            Container
                .Add<Func<TMarketConnector>>(sp => sp.Resolve<TMarketConnector>)
                .AsKeyed<Func<IMarketConnector>, ProviderKey>(providerKey)
                .In(_lifetime);
            Container
                .Add<Func<TUserConnector>>(sp => sp.Resolve<TUserConnector>)
                .AsKeyed<Func<IUserConnector>, ProviderKey>(providerKey)
                .In(_lifetime);
            Container
                .Add<Func<TFinanceService>>(sp => sp.Resolve<TFinanceService>)
                .AsKeyed<Func<IFinanceService>, ProviderKey>(providerKey)
                .In(_lifetime);
        }

        return this;
    }
}
