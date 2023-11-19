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

    public ProviderRegistrationContext(IServiceContainer container)
    {
        Container = container;
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
        Container.Add<TMarketProvider>().AsKeyed<IMarketProvider, string>(provider).AsSelf().Singleton();
        Container.Add<TMarketConnector>().AsSelf().Transient();
        Container.Add<TUserProvider>().AsKeyed<IUserProvider, string>(provider).AsSelf().Singleton();
        Container.Add<TUserConnector>().AsSelf().Transient();
        Container.Add<TFinanceService>().AsSelf().Transient();

        foreach (var env in Enum.GetValues<ProviderEnvironment>().Where(x => environment.HasFlag(x)))
        {
            var providerKey = ProviderKey.Create(provider, env);
            Container.Add(providerKey).AsSelf().Singleton();
            Container
                .Add<Func<IServiceProvider, TMarketConnector>>(sp => sp.Resolve<TMarketConnector>())
                .AsKeyed<Func<IServiceProvider, IMarketConnector>, ProviderKey>(providerKey)
                .Singleton();
            Container
                .Add<Func<IServiceProvider, TUserConnector>>(sp => sp.Resolve<TUserConnector>())
                .AsKeyed<Func<IServiceProvider, IUserConnector>, ProviderKey>(providerKey)
                .Singleton();
            Container
                .Add<Func<TFinanceService>>(sp => sp.Resolve<TFinanceService>)
                .AsKeyed<Func<IFinanceService>, ProviderKey>(providerKey)
                .Singleton();
        }

        return this;
    }
}
