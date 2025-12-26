using System.Collections.Generic;
using System.Net.Mime;
using Annium.Finance.Providers.Abstractions.Connectors.Market;
using Annium.Finance.Providers.Abstractions.Domain.Market;
using Annium.Finance.Providers.Core.Market;
using Annium.Finance.Providers.Core.Shared.Loaders;
using Annium.Finance.Providers.Core.Shared.Status;
using Annium.Finance.Providers.Crypto.Binance.Base.Market;
using Annium.Finance.Providers.Crypto.Binance.Base.Market.Services;
using Annium.Logging;
using Annium.Serialization.Abstractions;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Market;

internal class MarketConnector : MarketConnectorBase, IMarketConnector
{
    private readonly IBookTickerService _bookTickerService;

    public MarketConnector(
        MarketConfig config,
        IMarketProviderFactory providerFactory,
        ILoaderFactory loaderFactory,
        IBookTickerServiceFactory bookTickerServiceFactory,
        IStatusReporter reporter,
        IStatusMonitor monitor,
        ILogger logger
    )
        : base(config.GetSettings(), providerFactory, reporter, monitor, logger)
    {
        var exchangeInfoLoader = loaderFactory.CreateCompositeLoader<MarketContext>(
            new CompositeLoaderConfig(3000, 5, 10000, 600_000, 0),
            async _ => await Provider.LoadContextAsync()
        );
        Disposable += exchangeInfoLoader;
        exchangeInfoLoader.OnData += HandleMarketContext;
        Disposable += () => exchangeInfoLoader.OnData -= HandleMarketContext;
        exchangeInfoLoader.Start(true);

        Disposable += _bookTickerService = bookTickerServiceFactory.Create(
            config,
            SerializerKey.Create(Constants.InstrumentTickerKey, MediaTypeNames.Application.Json)
        );
        _bookTickerService.OnData += Write;
        Disposable += () => _bookTickerService.OnData -= Write;
    }

    public void SubscribeTickers(IReadOnlyCollection<string> symbols)
    {
        _bookTickerService.Subscribe(symbols);
    }

    public void UnsubscribeTickers(IReadOnlyCollection<string> symbols)
    {
        _bookTickerService.Unsubscribe(symbols);
    }

    private void HandleMarketContext(MarketContext ctx)
    {
        this.Trace("start");

        this.Trace(
            "schedule sync with {resources} resources and {instruments} instruments",
            ctx.Resources.Count,
            ctx.Instruments.Count
        );
        ScheduleSync(ctx.Resources, ctx.Instruments);

        this.Trace("done");
    }
}
