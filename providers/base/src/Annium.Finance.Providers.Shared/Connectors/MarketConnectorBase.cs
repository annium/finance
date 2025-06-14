using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;
using Annium.Execution.Background;
using Annium.Finance.Providers.Abstractions.Connectors.Connectors;
using Annium.Finance.Providers.Abstractions.Domain.Enums;
using Annium.Finance.Providers.Abstractions.Domain.Models;
using Annium.Logging;

namespace Annium.Finance.Providers.Shared.Connectors;

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
    protected readonly ChannelWriter<InstrumentTicker> TickerWriter;
    protected AsyncDisposableBox Disposable;
    private readonly IExecutor _executor;
    private readonly MarketSettings _settings;

    protected MarketConnectorBase(
        string provider,
        ProviderEnvironment environment,
        IStatusMonitor monitor,
        ILogger logger
    )
    {
        Logger = logger;
        _settings = new MarketSettings { Provider = provider, Environment = environment };

        Disposable = Annium.Disposable.AsyncBox(logger);

        Status = monitor.Status;

        monitor.OnStatusChanged += HandleStatusChanged;
        Disposable += () => monitor.OnStatusChanged -= HandleStatusChanged;

        monitor.OnError += HandleError;
        Disposable += () => monitor.OnError -= HandleError;

        var tickerChannel = Channel.CreateUnbounded<InstrumentTicker>();
        TickerWriter = tickerChannel.Writer;
        Tickers = tickerChannel.Reader.AsObservable().Publish().RefCount();

        Disposable += _executor = Executor.Sequential<MarketConnectorBase>(logger).Start();
    }

    public async ValueTask DisposeAsync()
    {
        this.Trace("{settings} start", _settings);

        await Disposable.DisposeAsync();

        this.Trace("{settings} done", _settings);
    }

    protected void ScheduleSync(
        IReadOnlyCollection<ResourceModel> resources,
        IReadOnlyCollection<InstrumentModel> instruments
    )
    {
        this.Trace("{settings} start", _settings);

        var scheduled = _executor.Schedule(async () =>
        {
            this.Trace("{settings} start sync", _settings);
            Resources = resources;
            Instruments = instruments;
            await OnSync(_settings, resources, instruments);
            this.Trace("{settings} done sync", _settings);
        });

        this.Trace("{settings} done, result: {result}", _settings, scheduled);
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
