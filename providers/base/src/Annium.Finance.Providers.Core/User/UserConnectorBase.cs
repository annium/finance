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

public abstract class UserConnectorBase : IAsyncDisposable, ILogSubject
{
    public ILogger Logger { get; }
    public ConnectorStatus Status { get; private set; }
    public IObservable<ChangeEvent<AssetModel>> Assets { get; }
    public IObservable<ChangeEvent<PositionModel>> Positions { get; }
    public IObservable<ChangeEvent<OrderModel>> Orders { get; }
    public IObservable<TradeModel> Trades { get; }
    public event Action<ConnectorStatus> OnStatusChanged = delegate { };
    public event Action<ConnectorError> OnError = delegate { };
    public event Func<UserSettings, IUserProvider, Task> OnSync = delegate
    {
        return Task.CompletedTask;
    };
    protected readonly string Id;
    protected readonly IUserProvider Provider;
    private readonly UserSettings _settings;
    protected AsyncDisposableBox Disposable;
    private readonly ChannelPair<ChangeEvent<AssetModel>> _assets;
    private readonly ChannelPair<ChangeEvent<PositionModel>> _positions;
    private readonly ChannelPair<ChangeEvent<OrderModel>> _orders;
    private readonly ChannelPair<TradeModel> _trades;
    private readonly IExecutor _executor;
    private readonly IStatusReporter _reporter;
    private AsyncDisposableBox _sourceSubscriptions;

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
        Disposable += _executor = Executor.Sequential<MarketConnectorBase>(logger).Start();

        // source subscriptions
        Disposable += _sourceSubscriptions = Annium.Disposable.AsyncBox(logger);
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

    protected void Write(ChangeEvent<AssetModel> asset) => _assets.Write(asset);

    protected void Write(ChangeEvent<PositionModel> position) => _positions.Write(position);

    protected void Write(ChangeEvent<OrderModel> order) => _orders.Write(order);

    protected void Write(TradeModel trade) => _trades.Write(trade);

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
            await UnsubscribeReadersAsync();

            this.Trace<string>("{id} sync start", Id);
            await OnSync(_settings, Provider);
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

    private void HandleError(ConnectorError error)
    {
        OnError(error);
    }

    private void SubscribeReaders()
    {
        _sourceSubscriptions += _assets.Connect();
        _sourceSubscriptions += _positions.Connect();
        _sourceSubscriptions += _orders.Connect();
        _sourceSubscriptions += _trades.Connect();
    }

    private async ValueTask UnsubscribeReadersAsync()
    {
        await _sourceSubscriptions.DisposeAndResetAsync();
    }
}
