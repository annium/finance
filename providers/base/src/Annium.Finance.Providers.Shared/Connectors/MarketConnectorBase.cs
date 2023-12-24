using System;
using System.Threading.Tasks;
using Annium.Data.Tables;
using Annium.Execution.Background;
using Annium.Finance.Providers.Abstractions.Connectors.Connectors;
using Annium.Finance.Providers.Abstractions.Connectors.Sync;
using Annium.Finance.Providers.Abstractions.Domain.Dto;
using Annium.Finance.Providers.Abstractions.Domain.Enums;
using Annium.Finance.Providers.Abstractions.Domain.Models;
using Annium.Logging;

namespace Annium.Finance.Providers.Shared.Connectors;

public abstract class MarketConnectorBase : IAsyncDisposable, ILogSubject
{
    public ILogger Logger { get; }
    public ConnectorStatus Status { get; private set; } = ConnectorStatus.Disconnected;
    public ITableView<ResourceDto> Resources => ResourcesTable;
    public ITableView<InstrumentDto> Instruments => InstrumentsTable;
    public ITableView<InstrumentTicker> Tickers => TickersTable;
    public event Action<ConnectorStatus> OnStatusChanged = delegate { };
    public event Action<ConnectorError> OnError = delegate { };
    protected readonly ITable<ResourceDto> ResourcesTable;
    protected readonly ITable<InstrumentDto> InstrumentsTable;
    protected readonly ITable<InstrumentTicker> TickersTable;
    protected AsyncDisposableBox Disposable;
    private readonly IExecutor _executor;
    private readonly MarketSettings _config;
    private readonly IMarketSynchronizer _synchronizer;

    protected MarketConnectorBase(
        string provider,
        ProviderEnvironment environment,
        ITableFactory tableFactory,
        IStatusMonitor monitor,
        IMarketSynchronizer synchronizer,
        ILogger logger
    )
    {
        Logger = logger;
        _config = new MarketSettings(provider, environment);
        _synchronizer = synchronizer;

        Disposable = Annium.Disposable.AsyncBox(logger);

        monitor.OnStatusChanged += HandleStatusChanged;
        Disposable += () => monitor.OnStatusChanged -= HandleStatusChanged;

        monitor.OnError += HandleError;
        Disposable += () => monitor.OnError -= HandleError;

        Disposable += ResourcesTable = tableFactory
            .New<ResourceDto>()
            .Allow(TablePermission.All)
            .Key(x => x.Code.GetHashCode())
            .Set(ResourceHasChanged, ResourceUpdate)
            .Build();
        Disposable += InstrumentsTable = tableFactory
            .New<InstrumentDto>()
            .Allow(TablePermission.All)
            .Key(x => x.Symbol.GetHashCode())
            .Set(InstrumentHasChanged, InstrumentUpdate)
            .Build();
        Disposable += TickersTable = tableFactory
            .New<InstrumentTicker>()
            .Allow(TablePermission.All)
            .Key(x => x.Symbol.GetHashCode())
            .Set(TickerHasChanged, TickerUpdate)
            .Build();

        Disposable += _executor = Executor.Sequential<MarketConnectorBase>(logger).Start();
    }

    private bool ResourceHasChanged(ResourceDto a, ResourceDto b)
    {
        return a.Precision != b.Precision;
    }

    private void ResourceUpdate(ResourceDto source, ResourceDto value)
    {
        source.Update(value.Precision);
    }

    private bool InstrumentHasChanged(InstrumentDto a, InstrumentDto b)
    {
        return a.MinQty != b.MinQty
            || a.MaxQty != b.MaxQty
            || a.LotSize != b.LotSize
            || a.MinPrice != b.MinPrice
            || a.MaxPrice != b.MaxPrice
            || a.TickSize != b.TickSize
            || a.MinSum != b.MinSum
            || a.MaxSum != b.MaxSum
            || a.MaxOrders != b.MaxOrders;
    }

    private void InstrumentUpdate(InstrumentDto source, InstrumentDto value)
    {
        source.Update(
            value.MinQty,
            value.MaxQty,
            value.LotSize,
            value.MinPrice,
            value.MaxPrice,
            value.TickSize,
            value.MinSum,
            value.MaxSum,
            value.MaxOrders
        );
    }

    private bool TickerHasChanged(InstrumentTicker a, InstrumentTicker b)
    {
        return a.BidPrice != b.BidPrice || a.AskPrice != b.AskPrice;
    }

    private void TickerUpdate(InstrumentTicker source, InstrumentTicker value)
    {
        source.Update(value.BidPrice, value.AskPrice);
    }

    public async ValueTask DisposeAsync()
    {
        this.Trace("start");

        await Disposable.DisposeAsync();

        this.Trace("done");
    }

    protected void ScheduleSync()
    {
        this.Trace("start");

        var scheduled = _executor.TrySchedule(async () =>
        {
            this.Trace("start sync");
            await _synchronizer.ExecuteAsync(_config, ResourcesTable, InstrumentsTable);
            this.Trace("done sync");
        });

        this.Trace("done, result: {result}", scheduled);
    }

    private void HandleStatusChanged(ConnectorStatus status)
    {
        Status = status;
        OnStatusChanged(status);
    }

    private void HandleError(ConnectorError error)
    {
        OnError(error);
    }
}
