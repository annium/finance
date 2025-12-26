using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Annium.Execution.Background;
using Annium.Finance.Providers.Abstractions.Connectors.Market;
using Annium.Finance.Providers.Abstractions.Connectors.Shared;
using Annium.Finance.Providers.Abstractions.Domain.Market;
using Annium.Finance.Providers.Core.Internal.Shared.Channels;
using Annium.Finance.Providers.Core.Shared.Status;
using Annium.Logging;

namespace Annium.Finance.Providers.Core.Market;

public abstract class MarketConnectorBase : IAsyncDisposable, ILogSubject
{
    public ILogger Logger { get; }
    public ConnectorStatus Status { get; private set; }
    public IReadOnlyCollection<ResourceModel> Resources { get; private set; } = [];
    public IReadOnlyCollection<InstrumentModel> Instruments { get; private set; } = [];
    public IObservable<InstrumentTicker> Tickers { get; }
    public event Action<ConnectorStatus> OnStatusChanged = delegate { };
    public event Action<ConnectorError> OnError = delegate { };
    public event Func<
        MarketSettings,
        IReadOnlyCollection<ResourceModel>,
        IReadOnlyCollection<InstrumentModel>,
        Task
    > OnSync = delegate
    {
        return Task.CompletedTask;
    };
    protected readonly string Id;
    protected readonly IMarketProvider Provider;
    protected AsyncDisposableBox Disposable;
    private readonly MarketSettings _settings;
    private readonly ChannelPair<InstrumentTicker> _tickers;
    private readonly IExecutor _executor;
    private readonly IStatusReporter _reporter;
    private DisposableBox _sourceSubscriptions;

    protected MarketConnectorBase(
        MarketSettings settings,
        IMarketProvider provider,
        IStatusReporter reporter,
        IStatusMonitor monitor,
        ILogger logger
    )
    {
        Logger = logger;
        Id = settings.ToString();
        Provider = provider;
        _settings = settings;

        Disposable = Annium.Disposable.AsyncBox(logger);

        // monitor
        _reporter = reporter;
        _reporter.Bind(this, ConnectorStatus.Connected);

        Status = monitor.Status;

        monitor.OnStatusChanged += HandleStatusChanged;
        Disposable += () => monitor.OnStatusChanged -= HandleStatusChanged;

        monitor.OnError += HandleError;
        Disposable += () => monitor.OnError -= HandleError;

        // tickers
        _tickers = new ChannelPair<InstrumentTicker>(logger);
        Tickers = _tickers.Observable;
        Disposable += Tickers.Subscribe();

        // executor
        Disposable += _executor = Executor.Sequential<MarketConnectorBase>(logger).Start();

        // source subscriptions
        Disposable += _sourceSubscriptions = Annium.Disposable.Box(logger);
    }

    public async ValueTask DisposeAsync()
    {
        this.Trace<string>("{id} start", Id);

        await Disposable.DisposeAsync();

        this.Trace<string>("{id} done", Id);
    }

    public void Sync()
    {
        this.Trace("{id} signal {state} state", Id, ConnectorStatus.Connecting);
        _reporter.Connecting();

        this.Trace("{id} signal {state} state", Id, ConnectorStatus.Connected);
        _reporter.Connected();

        this.Trace("{id} done");
    }

    protected void Write(InstrumentTicker ticker) => _tickers.Write(ticker);

    private void HandleStatusChanged(ConnectorStatus status)
    {
        if (status is ConnectorStatus.Connected)
            HandleSync();
        else
            CompleteStatusChange(status);
    }

    private void HandleSync()
    {
        this.Trace<string>("{id} schedule sync", Id);
        var scheduled = _executor.Schedule(async () =>
        {
            this.Trace<string>("{id} unsubscribe readers", Id);
            UnsubscribeReaders();

            this.Trace<string>("{id} sync start", Id);
            await OnSync(_settings, Resources, Instruments);
            this.Trace<string>("{id} sync done", Id);

            this.Trace<string>("{id} subscribe readers", Id);
            SubscribeReaders();

            this.Trace<string>("{id} complete status change", Id);
            CompleteStatusChange(ConnectorStatus.Connected);

            this.Trace<string>("{id} done sync", Id);
        });

        this.Trace("{id} done, scheduled: {result}", Id, scheduled);
    }

    private void CompleteStatusChange(ConnectorStatus status)
    {
        this.Trace("{id} notify {status} status", Id, status);
        Status = status;
        OnStatusChanged(status);
        this.Trace("{id} update to {status} status", Id, status);
    }

    protected void ScheduleSync(
        IReadOnlyCollection<ResourceModel> resources,
        IReadOnlyCollection<InstrumentModel> instruments
    )
    {
        this.Trace<string>("{id} start", Id);
        Resources = resources;
        Instruments = instruments;

        Sync();

        this.Trace<string>("{id} done", Id);
    }

    private void HandleError(ConnectorError error)
    {
        OnError(error);
    }

    private void SubscribeReaders()
    {
        _sourceSubscriptions += _tickers.Connect();
    }

    private void UnsubscribeReaders()
    {
        _sourceSubscriptions.DisposeAndReset();
    }
}
