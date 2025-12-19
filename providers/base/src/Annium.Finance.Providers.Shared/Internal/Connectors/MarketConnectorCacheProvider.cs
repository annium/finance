using System;
using Annium.Core.DependencyInjection;
using Annium.Finance.Providers.Abstractions.Connectors.Market;
using Annium.Finance.Providers.Abstractions.Domain.Market;
using Annium.Logging;

namespace Annium.Finance.Providers.Shared.Internal.Connectors;

internal sealed class MarketConnectorCacheProvider : ConnectorCacheProvider<MarketSettings, IMarketConnector>
{
    public MarketConnectorCacheProvider(IServiceProvider sp, ILogger logger)
        : base(sp, logger) { }

    protected override void Inject(IServiceProvider scopeProvider, MarketSettings settings)
    {
        scopeProvider.Resolve<Injected<MarketSettings>>().Init(settings);
    }
}
