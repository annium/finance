using System.Collections.Generic;
using Annium.Finance.Providers.Abstractions.Connectors.Market;
using Annium.Finance.Providers.Abstractions.Domain.Market;
using Annium.Finance.Providers.Core.Market;
using Annium.Finance.Providers.Core.Shared.Loaders;
using Annium.Finance.Providers.Core.Shared.Status;
using Annium.Finance.Providers.Crypto.Binance.Base.Market;
using Annium.Finance.Providers.Crypto.Binance.Base.Market.Services;
using Annium.Logging;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Market;

/// <summary>
/// Binance USD-M futures implementation of <see cref="IMarketConnector"/>. Reloads instruments and resources
/// through a composite loader and streams best bid/ask ticker updates through the shared book ticker websocket
/// service.
/// </summary>
internal class MarketConnector : MarketConnectorBase, IMarketConnector
{
    /// <summary>The book ticker websocket service supplying live best bid/ask updates.</summary>
    private readonly IBookTickerService _bookTickerService;

    /// <summary>
    /// Initializes a new instance of the <see cref="MarketConnector"/> class, wiring the market context loader
    /// and the book ticker service into the connector's sync and ticker pipelines.
    /// </summary>
    /// <param name="config">The resolved market configuration.</param>
    /// <param name="provider">The market provider used to load instruments and candles.</param>
    /// <param name="marketContextLoader">Loader that periodically reloads the resource/instrument context.</param>
    /// <param name="bookTickerService">The book ticker websocket service.</param>
    /// <param name="reporter">Reports connector status transitions.</param>
    /// <param name="monitor">Monitors connector status.</param>
    /// <param name="disposable">Accumulates cleanup actions for the connector's lifetime.</param>
    /// <param name="logger">The logger.</param>
    public MarketConnector(
        MarketConfig config,
        IMarketProvider provider,
        ICompositeLoader<MarketContext> marketContextLoader,
        IBookTickerService bookTickerService,
        IStatusReporter reporter,
        IStatusMonitor monitor,
        AsyncDisposableBox disposable,
        ILogger logger
    )
        : base(config.GetSettings(), provider, reporter, monitor, disposable, logger)
    {
        marketContextLoader.OnData += HandleMarketContext;
        Disposable += () => marketContextLoader.OnData -= HandleMarketContext;
        marketContextLoader.Start(true);

        _bookTickerService = bookTickerService;
        _bookTickerService.OnData += Write;
        Disposable += () => _bookTickerService.OnData -= Write;
    }

    /// <summary>
    /// Subscribes to book ticker updates for the given instrument symbols.
    /// </summary>
    /// <param name="symbols">The instrument symbols to subscribe to.</param>
    public void SubscribeTickers(IReadOnlyCollection<string> symbols)
    {
        _bookTickerService.Subscribe(symbols);
    }

    /// <summary>
    /// Unsubscribes from book ticker updates for the given instrument symbols.
    /// </summary>
    /// <param name="symbols">The instrument symbols to unsubscribe from.</param>
    public void UnsubscribeTickers(IReadOnlyCollection<string> symbols)
    {
        _bookTickerService.Unsubscribe(symbols);
    }

    /// <summary>
    /// Schedules a sync with the resources and instruments loaded by the market context loader.
    /// </summary>
    /// <param name="ctx">The reloaded market context.</param>
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
