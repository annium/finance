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

/// <summary>
/// Builds fully-wired <see cref="MarketConnector"/> instances for the USD-M futures provider: resolves
/// configuration, the underlying provider, the market context loader and the book ticker service.
/// </summary>
/// <param name="sp">The service provider used to resolve dependencies.</param>
internal class MarketConnectorFactory(IServiceProvider sp) : IMarketConnectorInstanceFactory
{
    /// <summary>
    /// Creates a market connector for the given settings.
    /// </summary>
    /// <param name="settings">The market settings identifying the provider and environment.</param>
    /// <param name="disposable">Accumulates cleanup actions for the connector's lifetime.</param>
    /// <returns>A new, ready-to-use market connector.</returns>
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
