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

/// <summary>
/// Base implementation of an <see cref="IMarketConnector"/>-shaped connector: tracks connection status via a
/// shared <see cref="IStatusMonitor"/>, drives resync cycles on a dedicated sequential executor (unsubscribing
/// ticker readers, invoking <see cref="OnSync"/>, then resubscribing before reporting itself connected again),
/// and fans out ticker updates written by subclasses through <see cref="Tickers"/>. Subclasses drive the
/// specifics: connecting to the provider, calling <see cref="ScheduleSync"/> to (re)load resources and
/// instruments, and calling <see cref="Write"/> as tickers arrive.
/// </summary>
public abstract class MarketConnectorBase : IAsyncDisposable, ILogSubject
{
    /// <summary>Gets the logger instance.</summary>
    public ILogger Logger { get; }

    /// <summary>Gets the current connection status of the connector.</summary>
    public ConnectorStatus Status { get; private set; }

    /// <summary>Gets the resources (assets) currently known to the connector, as loaded on the last sync.</summary>
    public IReadOnlyCollection<ResourceModel> Resources { get; private set; } = [];

    /// <summary>Gets the instruments currently known to the connector, as loaded on the last sync.</summary>
    public IReadOnlyCollection<InstrumentModel> Instruments { get; private set; } = [];

    /// <summary>
    /// An observable stream of instrument ticker updates. A ticker arrives whenever a subclass calls
    /// <see cref="Write"/> for a symbol the connector is currently subscribed to.
    /// </summary>
    public IObservable<InstrumentTicker> Tickers { get; }

    /// <summary>Raised whenever <see cref="Status"/> changes, with the new status.</summary>
    public event Action<ConnectorStatus> OnStatusChanged = delegate { };

    /// <summary>
    /// Raised when the connector encounters an error, e.g. a failed request or an unexpected disconnect. Does not
    /// necessarily imply a status change.
    /// </summary>
    public event Action<ConnectorError> OnError = delegate { };

    /// <summary>
    /// Raised during a sync cycle, once resources and instruments have been reloaded and before the connector
    /// resumes ticker subscriptions and reports itself as connected. Handlers can use this to re-subscribe to
    /// tickers for the refreshed instrument set; the connector waits for the handler to complete.
    /// </summary>
    public event Func<
        MarketSettings,
        IReadOnlyCollection<ResourceModel>,
        IReadOnlyCollection<InstrumentModel>,
        Task
    > OnSync = delegate
    {
        return Task.CompletedTask;
    };

    /// <summary>The connector's id, derived from its settings.</summary>
    protected readonly string Id;

    /// <summary>The underlying market provider this connector fetches data through.</summary>
    protected readonly IMarketProvider Provider;

    /// <summary>The disposable box collecting every resource this connector owns, disposed together on <see cref="DisposeAsync"/>.</summary>
    protected AsyncDisposableBox Disposable;

    /// <summary>The settings this connector was created with.</summary>
    private readonly MarketSettings _settings;

    /// <summary>The channel pair that fans ticker updates written by subclasses out through <see cref="Tickers"/>.</summary>
    private readonly ChannelPair<InstrumentTicker> _tickers;

    /// <summary>The sequential executor used to run resync cycles one at a time.</summary>
    private readonly IExecutor _executor;

    /// <summary>The status reporter this connector's connection status is bound to.</summary>
    private readonly IStatusReporter _reporter;

    /// <summary>The disposable box collecting the current sync cycle's ticker subscription, reset on every resync.</summary>
    private AsyncDisposableBox _sourceSubscriptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="MarketConnectorBase"/> class, binding its connection status
    /// to <paramref name="monitor"/> via <paramref name="reporter"/> and wiring up the ticker channel and the
    /// sequential executor used to run resync cycles.
    /// </summary>
    /// <param name="settings">The market settings identifying the provider and market to connect to.</param>
    /// <param name="provider">The underlying market provider this connector fetches data through.</param>
    /// <param name="reporter">The status reporter to bind this connector's connection status to.</param>
    /// <param name="monitor">The shared status monitor this connector's initial status and status/error notifications come from.</param>
    /// <param name="disposable">The disposable box this connector adds its owned resources to.</param>
    /// <param name="logger">The logger instance.</param>
    protected MarketConnectorBase(
        MarketSettings settings,
        IMarketProvider provider,
        IStatusReporter reporter,
        IStatusMonitor monitor,
        AsyncDisposableBox disposable,
        ILogger logger
    )
    {
        Logger = logger;
        Id = settings.ToString();
        Provider = provider;
        _settings = settings;

        Disposable = disposable;

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
        Disposable += _sourceSubscriptions = Annium.Disposable.AsyncBox(logger);
    }

    /// <summary>
    /// Disposes every resource collected in <see cref="Disposable"/>.
    /// </summary>
    /// <returns>A task that completes once disposal has finished.</returns>
    public async ValueTask DisposeAsync()
    {
        this.Trace<string>("{id} start", Id);

        await Disposable.DisposeAsync();

        this.Trace<string>("{id} done", Id);
    }

    /// <summary>
    /// Forces a resync: reports connecting then connected status through <see cref="_reporter"/>, which drives
    /// the connector through <see cref="HandleStatusChanged"/> and <see cref="HandleSync"/> the same way an
    /// externally-driven status change would.
    /// </summary>
    public void Sync()
    {
        this.Trace("{id} signal {state} state", Id, ConnectorStatus.Connecting);
        _reporter.Connecting();

        this.Trace("{id} signal {state} state", Id, ConnectorStatus.Connected);
        _reporter.Connected();

        this.Trace("{id} done");
    }

    /// <summary>
    /// Writes a ticker update into the connector's ticker channel, to be forwarded through <see cref="Tickers"/>.
    /// </summary>
    /// <param name="ticker">The ticker update to write.</param>
    protected void Write(InstrumentTicker ticker) => _tickers.Write(ticker);

    /// <summary>
    /// Handles a status change from the shared monitor: a transition to connected triggers a resync cycle via
    /// <see cref="HandleSync"/>; any other status is applied directly via <see cref="CompleteStatusChange"/>.
    /// </summary>
    /// <param name="status">The new status reported by the monitor.</param>
    private void HandleStatusChanged(ConnectorStatus status)
    {
        if (status is ConnectorStatus.Connected)
            HandleSync();
        else
            CompleteStatusChange(status);
    }

    /// <summary>
    /// Schedules a resync cycle on the sequential executor: unsubscribes ticker readers, invokes
    /// <see cref="OnSync"/> to let the subclass reload resources/instruments and resubscribe, then resubscribes
    /// ticker readers and reports the connector as connected.
    /// </summary>
    private void HandleSync()
    {
        this.Trace<string>("{id} schedule sync", Id);
        var scheduled = _executor.Schedule(async () =>
        {
            this.Trace<string>("{id} unsubscribe readers", Id);
            await UnsubscribeReadersAsync();

            this.Trace<string>("{id} sync start", Id);
            try
            {
                await OnSync(_settings, Resources, Instruments);
            }
            catch (Exception e)
            {
                // the executor running this catches and logs, so without this the failure ends in a log
                // line: the readers stay unsubscribed from the step above, the status never completes, and
                // nothing tells the caller why the connector went quiet
                this.Error(e);
                OnError(new ConnectorError($"sync failed: {e.Message}"));
                CompleteStatusChange(ConnectorStatus.Connecting);

                return;
            }

            this.Trace<string>("{id} sync done", Id);

            this.Trace<string>("{id} subscribe readers", Id);
            SubscribeReaders();

            this.Trace<string>("{id} complete status change", Id);
            CompleteStatusChange(ConnectorStatus.Connected);

            this.Trace<string>("{id} done sync", Id);
        });

        this.Trace("{id} done, scheduled: {result}", Id, scheduled);
    }

    /// <summary>
    /// Applies a new status: updates <see cref="Status"/> and raises <see cref="OnStatusChanged"/>.
    /// </summary>
    /// <param name="status">The new status to apply.</param>
    private void CompleteStatusChange(ConnectorStatus status)
    {
        this.Trace("{id} notify {status} status", Id, status);
        Status = status;
        OnStatusChanged(status);
        this.Trace("{id} update to {status} status", Id, status);
    }

    /// <summary>
    /// Updates <see cref="Resources"/> and <see cref="Instruments"/> with freshly loaded data and forces a
    /// resync via <see cref="Sync"/> so <see cref="OnSync"/> handlers observe the new values.
    /// </summary>
    /// <param name="resources">The freshly loaded resources.</param>
    /// <param name="instruments">The freshly loaded instruments.</param>
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

    /// <summary>
    /// Forwards an error from the shared monitor through <see cref="OnError"/>.
    /// </summary>
    /// <param name="error">The error reported by the monitor.</param>
    private void HandleError(ConnectorError error)
    {
        OnError(error);
    }

    /// <summary>
    /// Connects the ticker channel so writes made via <see cref="Write"/> start flowing through <see cref="Tickers"/>.
    /// </summary>
    private void SubscribeReaders()
    {
        _sourceSubscriptions += _tickers.Connect();
    }

    /// <summary>
    /// Disposes the current sync cycle's ticker subscription, stopping tickers from flowing through
    /// <see cref="Tickers"/> until <see cref="SubscribeReaders"/> is called again.
    /// </summary>
    /// <returns>A task that completes once the subscription has been disposed.</returns>
    private async ValueTask UnsubscribeReadersAsync()
    {
        await _sourceSubscriptions.DisposeAndResetAsync();
    }
}
