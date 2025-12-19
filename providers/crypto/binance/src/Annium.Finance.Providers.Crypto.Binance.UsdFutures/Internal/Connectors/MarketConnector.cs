using System.Collections.Generic;
using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Connectors.Market;
using Annium.Finance.Providers.Abstractions.Domain.Market;
using Annium.Finance.Providers.Crypto.Binance.Base.Services;
using Annium.Finance.Providers.Shared.Connectors;
using Annium.Finance.Providers.Shared.Loaders;
using Annium.Logging;
using Annium.Threading.Channels;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Connectors;

internal class MarketConnector : MarketConnectorBase, IMarketConnector
{
    private readonly BookTickerService _bookTickerService;

    public MarketConnector(
        MarketConfig config,
        MarketProvider marketProvider,
        ILoaderFactory loaderFactory,
        BookTickerService bookTickerService,
        IStatusMonitor monitor,
        ILogger logger
    )
        : base(config.Provider, config.Environment, monitor, logger)
    {
        var exchangeInfoLoader = loaderFactory.CreateCompositeLoader<MarketContext>(
            new CompositeLoaderConfig(3000, 5, 10000, 600_000, 0),
            async _ => await marketProvider.LoadContextAsync(config.Environment)
        );
        Disposable += exchangeInfoLoader;
        exchangeInfoLoader.OnData += HandleMarketContext;
        Disposable += () => exchangeInfoLoader.OnData -= HandleMarketContext;
        exchangeInfoLoader.Start(true);

        _bookTickerService = bookTickerService;
        _bookTickerService.OnData += HandleTicker;
        Disposable += () => _bookTickerService.OnData -= HandleTicker;
    }

    public ValueTask InitAsync()
    {
        return ValueTask.CompletedTask;
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

    private void HandleTicker(InstrumentTicker ticker)
    {
        TickerWriter.Write(ticker);
    }
}
