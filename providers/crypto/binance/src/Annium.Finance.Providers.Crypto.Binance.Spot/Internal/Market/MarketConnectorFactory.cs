using System;
using Annium.Core.DependencyInjection;
using Annium.Finance.Providers.Abstractions.Connectors.Market;
using Annium.Finance.Providers.Abstractions.Domain.Market;
using Annium.Finance.Providers.Core.Shared.Loaders;
using Annium.Finance.Providers.Core.Shared.Status;
using Annium.Finance.Providers.Crypto.Binance.Base.Market.Services;
using Annium.Finance.Providers.Crypto.Binance.Spot.Internal.Shared;
using Annium.Logging;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Internal.Market;

internal class MarketConnectorFactory(IServiceProvider sp) : IMarketConnectorFactory
{
    public IMarketConnector Create(MarketSettings settings)
    {
        var config = CreateConfig(settings);

        var providerFactory = sp.ResolveKeyed<IMarketProviderFactory>(config.Provider);
        var loaderFactory = sp.Resolve<ILoaderFactory>();
        var bookTickerServiceFactory = sp.Resolve<IBookTickerServiceFactory>();
        var monitor = sp.Resolve<IStatusMonitor>();
        var logger = sp.Resolve<ILogger>();

        return new MarketConnector(config, providerFactory, loaderFactory, bookTickerServiceFactory, monitor, logger);
    }

    private static MarketConfig CreateConfig(MarketSettings settings)
    {
        var httpApi = Endpoints.GetHttpApi(settings.Environment);
        var wsApi = Endpoints.GetWsApi(settings.Environment);

        return new MarketConfig
        {
            Provider = settings.Provider,
            Environment = settings.Environment,
            HttpApi = httpApi,
            WsApi = wsApi,
            WsUriPath = "/stream",
        };
    }
}
