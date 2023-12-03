using System.Collections.Generic;
using System.Threading.Tasks;
using Annium.Data.Tables;
using Annium.Finance.Providers.Abstractions.Connectors.Connectors;
using Annium.Finance.Providers.Abstractions.Connectors.Sync;
using Annium.Finance.Providers.Abstractions.Domain.Models;
using Annium.Finance.Providers.Crypto.Binance.Base;
using Annium.Finance.Providers.Crypto.Binance.Base.Connectors;
using Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Services;
using Annium.Finance.Providers.Shared.Connectors;
using Annium.Finance.Providers.Shared.Internal.Services;
using Annium.Finance.Providers.Shared.Services;
using Annium.Logging;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Connectors;

internal class MarketConnector : MarketConnectorBase, IMarketConnector
{
    private readonly BookTickerService _bookTickerService;

    public MarketConnector(
        ConfigurationBase config,
        ITableFactory tableFactory,
        MarketProvider marketProvider,
        ILoaderFactory loaderFactory,
        BookTickerService bookTickerService,
        IStatusMonitor monitor,
        IMarketSynchronizer synchronizer,
        ILogger logger
    )
        : base(config.Provider, config.Environment, tableFactory, monitor, synchronizer, logger)
    {
        var exchangeInfoLoader = loaderFactory.CreateCompositeLoader<MarketContext>(
            new SnapshotLoaderConfig(3000, 10000, 5),
            async _ => await marketProvider.LoadContextAsync(config.Environment),
            600_000,
            0
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

        this.Trace("init {count} resources", ctx.Resources.Count);
        ResourcesTable.Init(ctx.Resources);

        this.Trace("init {count} instruments", ctx.Instruments.Count);
        InstrumentsTable.Init(ctx.Instruments);

        ScheduleSync();

        this.Trace("done");
    }

    private void HandleTicker(InstrumentTicker ticker)
    {
        this.Trace("start");

        this.Trace("set {ticker}", ticker);
        TickersTable.Set(ticker);

        this.Trace("done");
    }
}
