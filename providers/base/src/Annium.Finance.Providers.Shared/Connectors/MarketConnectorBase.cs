using System;
using System.Threading;
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
    public event Action<ConnectorStatus> OnStatusChanged = delegate { };
    public ILogger Logger { get; }
    public ITableView<ResourceDto> Resources => ResourcesTable;
    public ITableView<InstrumentDto> Instruments => InstrumentsTable;
    public ITableView<InstrumentTicker> Tickers => TickersTable;
    protected readonly ITable<ResourceDto> ResourcesTable;
    protected readonly ITable<InstrumentDto> InstrumentsTable;
    protected readonly ITable<InstrumentTicker> TickersTable;
    protected AsyncDisposableBox Disposable;
    private readonly IExecutor _executor;
    private readonly MarketSettings _config;
    private readonly IMarketSynchronizer _synchronizer;
    private int _isDisposed;

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

        Disposable += ResourcesTable = tableFactory
            .New<ResourceDto>()
            .Allow(TablePermission.All)
            .Key(x => x.Code)
            .Build();
        Disposable += InstrumentsTable = tableFactory
            .New<InstrumentDto>()
            .Allow(TablePermission.All)
            .Key(x => x.Symbol)
            .Build();
        Disposable += TickersTable = tableFactory
            .New<InstrumentTicker>()
            .Allow(TablePermission.All)
            .Key(x => x.Symbol)
            .Build();

        Disposable += _executor = Executor.Sequential<MarketConnectorBase>(logger).Start();
    }

    public async ValueTask DisposeAsync()
    {
        this.Trace("start");
        if (Interlocked.CompareExchange(ref _isDisposed, 1, 0) == 1)
        {
            this.Trace("already disposed");
            return;
        }

        this.Trace("run");
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

    private void HandleStatusChanged(ConnectorStatus status) => OnStatusChanged(status);
}
