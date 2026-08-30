using System.Collections.Generic;
using Annium.Finance.Providers.Abstractions.Connectors.Market;
using Annium.Finance.Providers.Abstractions.Domain.Market;
using Annium.Finance.Providers.Core.Market;
using Annium.Finance.Providers.Core.Shared.Loaders;
using Annium.Finance.Providers.Core.Shared.Status;
using Annium.Finance.Providers.Crypto.Binance.Base.Market;
using Annium.Finance.Providers.Crypto.Binance.Base.Market.Services;
using Annium.Logging;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Internal.Market;

/// <summary>
/// Binance spot market data connector: reloads resources and instruments from the exchange info endpoint and
/// streams book ticker updates for the subscribed symbols.
/// </summary>
internal class MarketConnector : MarketConnectorBase, IMarketConnector
{
    /// <summary>The book ticker (best bid/ask) subscription service backing the ticker stream.</summary>
    private readonly IBookTickerService _bookTickerService;

    /// <summary>
    /// Initializes a new instance of the <see cref="MarketConnector"/> class, wiring the composite context
    /// loader and the book ticker service into the base connector's sync and ticker pipelines.
    /// </summary>
    /// <param name="config">The resolved market connection settings.</param>
    /// <param name="provider">The market data provider used to load resources, instruments and candles.</param>
    /// <param name="marketContextLoader">The loader that periodically reloads resources and instruments.</param>
    /// <param name="bookTickerService">The book ticker subscription service.</param>
    /// <param name="reporter">The status reporter used to publish connection status changes.</param>
    /// <param name="monitor">The status monitor used to detect and recover from stalled connections.</param>
    /// <param name="disposable">The disposable box collecting this connector's cleanup actions.</param>
    /// <param name="logger">The logger instance.</param>
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

    /// <summary>Subscribes to book ticker updates for the given instrument symbols.</summary>
    /// <param name="symbols">The instrument symbols to subscribe to.</param>
    public void SubscribeTickers(IReadOnlyCollection<string> symbols)
    {
        _bookTickerService.Subscribe(symbols);
    }

    /// <summary>Unsubscribes from book ticker updates for the given instrument symbols.</summary>
    /// <param name="symbols">The instrument symbols to unsubscribe from.</param>
    public void UnsubscribeTickers(IReadOnlyCollection<string> symbols)
    {
        _bookTickerService.Unsubscribe(symbols);
    }

    /// <summary>Handles a freshly loaded market context by scheduling a resync with its resources and instruments.</summary>
    /// <param name="ctx">The reloaded resources and instruments.</param>
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
