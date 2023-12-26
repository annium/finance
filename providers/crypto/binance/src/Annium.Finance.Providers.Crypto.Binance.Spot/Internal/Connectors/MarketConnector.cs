using System.Collections.Generic;
using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Connectors.Connectors;
using Annium.Finance.Providers.Abstractions.Domain.Models;
using Annium.Finance.Providers.Crypto.Binance.Base.Connectors;
using Annium.Finance.Providers.Crypto.Binance.Base.Services;
using Annium.Finance.Providers.Shared.Connectors;
using Annium.Finance.Providers.Shared.Services;
using Annium.Logging;
using Annium.Threading.Channels;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Internal.Connectors;

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
        exchangeInfoLoader.Start();

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
        this.Trace("start");

        this.Trace("write {ticker}", ticker);
        TickerWriter.Write(ticker);

        this.Trace("done");
    }
}
