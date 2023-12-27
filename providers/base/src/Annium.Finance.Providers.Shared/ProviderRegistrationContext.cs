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
        TQueryProcessor,
        TUserConnector,
        TFinanceService
    >(string provider, ProviderEnvironment environment)
        where TMarketProvider : IMarketProvider
        where TMarketConnector : IMarketConnector
        where TUserProvider : IUserProvider
        where TUserConnector : IUserConnector
        where TQueryProcessor : IQueryProcessor
        where TFinanceService : IFinanceService
    {
        Container.Add<TMarketProvider>().AsKeyed<IMarketProvider>(provider).AsSelf().Singleton();
        Container.Add<TMarketConnector>().AsSelf().Transient();
        Container.Add<TUserProvider>().AsKeyed<IUserProvider>(provider).AsSelf().Singleton();
        Container.Add<TQueryProcessor>().AsSelf().Singleton();
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
        }

        return this;
    }
}
