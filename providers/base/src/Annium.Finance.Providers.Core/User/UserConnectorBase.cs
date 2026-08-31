using System;
using System.Threading.Tasks;
using Annium.Data.Tables;
using Annium.Execution.Background;
using Annium.Finance.Providers.Abstractions.Connectors.Shared;
using Annium.Finance.Providers.Abstractions.Connectors.User;
using Annium.Finance.Providers.Abstractions.Domain.User;
using Annium.Finance.Providers.Core.Internal.Shared.Channels;
using Annium.Finance.Providers.Core.Market;
using Annium.Finance.Providers.Core.Shared.Status;
using Annium.Logging;

namespace Annium.Finance.Providers.Core.User;

/// <summary>
/// Base implementation of an <see cref="IUserConnector"/>-shaped connector: tracks connection status via a
/// shared <see cref="IStatusMonitor"/>, drives resync cycles on a dedicated sequential executor (unsubscribing
/// account state readers, invoking <see cref="OnSync"/>, then resubscribing before reporting itself connected
/// again), and fans out account state updates written by subclasses through <see cref="Assets"/>,
/// <see cref="Positions"/>, <see cref="Orders"/>, and <see cref="Trades"/>. Subclasses drive the specifics:
/// connecting to the provider, and calling the appropriate <c>Write</c> overload as updates arrive.
/// </summary>
public abstract class UserConnectorBase : IAsyncDisposable, ILogSubject
{
    /// <summary>Gets the logger instance.</summary>
    public ILogger Logger { get; }

    /// <summary>Gets the current connection status of the connector.</summary>
    public ConnectorStatus Status { get; private set; }

    /// <summary>
    /// An observable stream of asset balance changes. Emits an <c>Init</c> event with the full asset set on
    /// (re)sync, and <c>Set</c>/<c>Delete</c> events as individual asset balances change afterwards.
    /// </summary>
    public IObservable<ChangeEvent<AssetModel>> Assets { get; }

    /// <summary>
    /// An observable stream of position changes. Emits an <c>Init</c> event with the full position set on
    /// (re)sync, and <c>Set</c>/<c>Delete</c> events as individual positions change afterwards.
    /// </summary>
    public IObservable<ChangeEvent<PositionModel>> Positions { get; }

    /// <summary>
    /// An observable stream of order changes. Emits an <c>Init</c> event with the currently open orders on
    /// (re)sync, then a <c>Set</c> event whenever an order is placed or updated while still open, and a
    /// <c>Delete</c> event once it stops being open (filled, canceled, rejected, expired).
    /// </summary>
    public IObservable<ChangeEvent<OrderModel>> Orders { get; }

    /// <summary>
    /// An observable stream of executed trades. Emits a trade as soon as it is reported by the provider.
    /// </summary>
    public IObservable<TradeModel> Trades { get; }

    /// <summary>Raised whenever <see cref="Status"/> changes, with the new status.</summary>
    public event Action<ConnectorStatus> OnStatusChanged = delegate { };

    /// <summary>
    /// Raised when the connector encounters an error, e.g. a failed request or an unexpected disconnect. Does not
    /// necessarily imply a status change.
    /// </summary>
    public event Action<ConnectorError> OnError = delegate { };

    /// <summary>
    /// Raised during a sync cycle, after the connector stops forwarding real-time updates and before it resumes
    /// them and reports itself as connected. Handlers receive the active settings and the underlying provider so
    /// they can (re)load account state before the connector goes live; the connector waits for the handler to
    /// complete.
    /// </summary>
    public event Func<UserSettings, IUserProvider, Task> OnSync = delegate
    {
        return Task.CompletedTask;
    };

    /// <summary>The connector's id, derived from its settings.</summary>
    protected readonly string Id;

    /// <summary>The underlying user provider this connector fetches data through.</summary>
    protected readonly IUserProvider Provider;

    /// <summary>The settings this connector was created with.</summary>
    private readonly UserSettings _settings;

    /// <summary>The disposable box collecting every resource this connector owns, disposed together on <see cref="DisposeAsync"/>.</summary>
    protected AsyncDisposableBox Disposable;

    /// <summary>The channel pair that fans asset updates written by subclasses out through <see cref="Assets"/>.</summary>
    private readonly ChannelPair<ChangeEvent<AssetModel>> _assets;

    /// <summary>The channel pair that fans position updates written by subclasses out through <see cref="Positions"/>.</summary>
    private readonly ChannelPair<ChangeEvent<PositionModel>> _positions;

    /// <summary>The channel pair that fans order updates written by subclasses out through <see cref="Orders"/>.</summary>
    private readonly ChannelPair<ChangeEvent<OrderModel>> _orders;

    /// <summary>The channel pair that fans trade updates written by subclasses out through <see cref="Trades"/>.</summary>
    private readonly ChannelPair<TradeModel> _trades;

    /// <summary>The sequential executor used to run resync cycles one at a time.</summary>
    private readonly IExecutor _executor;

    /// <summary>The status reporter this connector's connection status is bound to.</summary>
    private readonly IStatusReporter _reporter;

    /// <summary>The disposable box collecting the current sync cycle's account state subscriptions, reset on every resync.</summary>
    private AsyncDisposableBox _sourceSubscriptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserConnectorBase"/> class, binding its connection status to
    /// <paramref name="monitor"/> via <paramref name="reporter"/> and wiring up the account state channels and
    /// the sequential executor used to run resync cycles.
    /// </summary>
    /// <param name="settings">The user settings identifying the provider and account to connect to.</param>
    /// <param name="provider">The underlying user provider this connector fetches data through.</param>
    /// <param name="reporter">The status reporter to bind this connector's connection status to.</param>
    /// <param name="monitor">The shared status monitor this connector's initial status and status/error notifications come from.</param>
    /// <param name="disposable">The disposable box this connector adds its owned resources to.</param>
    /// <param name="logger">The logger instance.</param>
    protected UserConnectorBase(
        UserSettings settings,
        IUserProvider provider,
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

        // and stop counting once disposed. Binding registers this connector as a target of the monitor,
        // and nothing else removes it - a disposed connector left registered sits there at whatever status
        // it last held, so the monitor goes on resolving an overall status from a component that is gone.
        // Every other component that binds a reporter was given this; these two were missed
        Disposable += () => _reporter.Unbind();

        Status = monitor.Status;

        monitor.OnStatusChanged += HandleStatusChanged;
        Disposable += () => monitor.OnStatusChanged -= HandleStatusChanged;

        monitor.OnError += HandleError;
        Disposable += () => monitor.OnError -= HandleError;

        // assets
        _assets = new ChannelPair<ChangeEvent<AssetModel>>(logger);
        Assets = _assets.Observable;
        Disposable += Assets.Subscribe();

        // positions
        _positions = new ChannelPair<ChangeEvent<PositionModel>>(logger);
        Positions = _positions.Observable;
        Disposable += Positions.Subscribe();

        // orders
        _orders = new ChannelPair<ChangeEvent<OrderModel>>(logger);
        Orders = _orders.Observable;
        Disposable += Orders.Subscribe();

        // trades
        _trades = new ChannelPair<TradeModel>(logger);
        Trades = _trades.Observable;
        Disposable += Trades.Subscribe();

        // executor
        Disposable += _executor = Executor.Sequential<UserConnectorBase>(logger).Start();

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
    /// Writes an asset balance change into the connector's asset channel, to be forwarded through <see cref="Assets"/>.
    /// </summary>
    /// <param name="asset">The asset change to write.</param>
    protected void Write(ChangeEvent<AssetModel> asset) => _assets.Write(asset);

    /// <summary>
    /// Writes a position change into the connector's position channel, to be forwarded through <see cref="Positions"/>.
    /// </summary>
    /// <param name="position">The position change to write.</param>
    protected void Write(ChangeEvent<PositionModel> position) => _positions.Write(position);

    /// <summary>
    /// Writes an order change into the connector's order channel, to be forwarded through <see cref="Orders"/>.
    /// </summary>
    /// <param name="order">The order change to write.</param>
    protected void Write(ChangeEvent<OrderModel> order) => _orders.Write(order);

    /// <summary>
    /// Writes an executed trade into the connector's trade channel, to be forwarded through <see cref="Trades"/>.
    /// </summary>
    /// <param name="trade">The trade to write.</param>
    protected void Write(TradeModel trade) => _trades.Write(trade);

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
    /// Schedules a resync cycle on the sequential executor: unsubscribes account state readers, invokes
    /// <see cref="OnSync"/> to let the subclass reload account state and resubscribe, then resubscribes account
    /// state readers and reports the connector as connected.
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
                await OnSync(_settings, Provider);
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
    /// Forwards an error from the shared monitor through <see cref="OnError"/>.
    /// </summary>
    /// <param name="error">The error reported by the monitor.</param>
    private void HandleError(ConnectorError error)
    {
        OnError(error);
    }

    /// <summary>
    /// Connects every account state channel so writes made via the <c>Write</c> overloads start flowing through
    /// <see cref="Assets"/>, <see cref="Positions"/>, <see cref="Orders"/>, and <see cref="Trades"/>.
    /// </summary>
    private void SubscribeReaders()
    {
        _sourceSubscriptions += _assets.Connect();
        _sourceSubscriptions += _positions.Connect();
        _sourceSubscriptions += _orders.Connect();
        _sourceSubscriptions += _trades.Connect();
    }

    /// <summary>
    /// Disposes the current sync cycle's account state subscriptions, stopping updates from flowing through
    /// <see cref="Assets"/>, <see cref="Positions"/>, <see cref="Orders"/>, and <see cref="Trades"/> until
    /// <see cref="SubscribeReaders"/> is called again.
    /// </summary>
    /// <returns>A task that completes once the subscriptions have been disposed.</returns>
    private async ValueTask UnsubscribeReadersAsync()
    {
        await _sourceSubscriptions.DisposeAndResetAsync();
    }
}
