using System;
using System.Net.Mime;
using Annium.Core.DependencyInjection;
using Annium.Core.Mapper;
using Annium.Finance.Providers.Abstractions.Connectors.Market;
using Annium.Finance.Providers.Abstractions.Domain.Market;
using Annium.Finance.Providers.Core.Shared.Loaders;
using Annium.Finance.Providers.Core.Shared.Status;
using Annium.Finance.Providers.Crypto.Binance.Base.Market.Services;
using Annium.Logging;
using Annium.Serialization.Abstractions;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Market;

internal class MarketConnectorFactory(IServiceProvider sp) : IMarketConnectorFactory
{
    public IMarketConnector Create(MarketSettings settings)
    {
        var config = sp.Resolve<IMapper>().Map<MarketConfig>(settings);

        var providerFactory = sp.ResolveKeyed<IMarketProviderFactory>(config.Provider);
        var provider = providerFactory.Create(settings);
        var loaderFactory = sp.Resolve<ILoaderFactory>();
        var bookTickerServiceFactory = sp.Resolve<IBookTickerServiceFactory>();
        var bookTickerService = bookTickerServiceFactory.Create(
            config,
            SerializerKey.Create(Constants.InstrumentTickerKey, MediaTypeNames.Application.Json)
        );
        var reporter = sp.Resolve<IStatusReporter>();
        var monitor = sp.Resolve<IStatusMonitor>();
        var logger = sp.Resolve<ILogger>();

        return new MarketConnector(config, provider, loaderFactory, bookTickerService, reporter, monitor, logger);
    }
}
