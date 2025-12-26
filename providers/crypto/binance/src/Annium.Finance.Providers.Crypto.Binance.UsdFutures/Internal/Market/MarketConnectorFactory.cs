using System;
using Annium.Core.DependencyInjection;
using Annium.Core.Mapper;
using Annium.Finance.Providers.Abstractions.Connectors.Market;
using Annium.Finance.Providers.Abstractions.Domain.Market;
using Annium.Finance.Providers.Core.Market;
using Annium.Finance.Providers.Core.Shared.Loaders;
using Annium.Finance.Providers.Core.Shared.Status;
using Annium.Finance.Providers.Crypto.Binance.Base.Market;
using Annium.Logging;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Market;

internal class MarketConnectorFactory(IServiceProvider sp) : IMarketConnectorInstanceFactory
{
    public IMarketConnector Create(MarketSettings settings, AsyncDisposableBox disposable)
    {
        var config = sp.Resolve<IMapper>().Map<MarketConfig>(settings);

        var provider = sp.CreateMarketProvider(settings);
        var marketContextLoader = sp.CreateMarketContextLoader(
            new CompositeLoaderConfig(3000, 5, 10000, 600_000, 0),
            provider,
            ref disposable
        );
        var bookTickerService = sp.CreateBookTickerService(config, Constants.InstrumentTickerKey, ref disposable);
        var reporter = sp.Resolve<IStatusReporter>();
        var monitor = sp.Resolve<IStatusMonitor>();
        var logger = sp.Resolve<ILogger>();

        return new MarketConnector(
            config,
            provider,
            marketContextLoader,
            bookTickerService,
            reporter,
            monitor,
            disposable,
            logger
        );
    }
}
