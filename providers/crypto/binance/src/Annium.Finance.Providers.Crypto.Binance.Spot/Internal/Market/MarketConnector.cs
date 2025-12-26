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

internal class MarketConnector : MarketConnectorBase, IMarketConnector
{
    private readonly IBookTickerService _bookTickerService;

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
