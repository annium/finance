using System;
using Annium.Core.DependencyInjection;
using Annium.Finance.Providers.Abstractions.Connectors.Market;
using Annium.Finance.Providers.Abstractions.Domain.Market;
using Annium.Finance.Providers.Abstractions.Domain.Shared;
using Annium.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace Annium.Finance.Providers.Core.Internal.Market;

internal class MarketConnectorFactory(IServiceProvider sp, ILogger logger) : IMarketConnectorFactory, ILogSubject
{
    public ILogger Logger { get; } = logger;

    public IMarketConnector Create(MarketSettings settings)
    {
        var providerKey = settings.GetProviderKey();

        this.Trace("{key} - create new scope for {settings}", providerKey, settings);
        var scope = sp.CreateAsyncScope();

        this.Trace("{key} - resolve factory for {settings}", providerKey, settings);
        var factory = scope.ServiceProvider.ResolveKeyed<IMarketConnectorFactory>(settings.Provider);

        this.Trace<ProviderKey, MarketSettings, string>(
            "{key} - create connector for {settings} with {factory}",
            providerKey,
            settings,
            factory.GetFullId()
        );
        var connector = factory.Create(settings);

        return new MarketConnectorContainer(scope, connector, Logger);
    }
}
