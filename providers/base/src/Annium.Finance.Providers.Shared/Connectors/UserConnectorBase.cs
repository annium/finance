using System;
using System.Reactive.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;
using Annium.Data.Tables;
using Annium.Execution.Background;
using Annium.Finance.Providers.Abstractions.Connectors.Connectors;
using Annium.Finance.Providers.Abstractions.Domain.Models;
using Annium.Logging;
using Annium.Threading.Channels;

namespace Annium.Finance.Providers.Shared.Connectors;

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
    protected readonly UserSettings Settings;
    protected readonly IUserProvider UserProvider;
    protected readonly ChannelWriter<ChangeEvent<AssetModel>> AssetWriter;
    protected readonly ChannelWriter<ChangeEvent<PositionModel>> PositionWriter;
    protected readonly ChannelWriter<ChangeEvent<OrderModel>> OrderWriter;
    protected readonly ChannelWriter<TradeModel> TradeWriter;
    protected AsyncDisposableBox Disposable;
    private readonly ChannelReader<ChangeEvent<AssetModel>> _assetSource;
    private readonly ChannelWriter<ChangeEvent<AssetModel>> _assetTarget;
    private readonly ChannelReader<ChangeEvent<PositionModel>> _positionSource;
    private readonly ChannelWriter<ChangeEvent<PositionModel>> _positionTarget;
    private readonly ChannelReader<ChangeEvent<OrderModel>> _orderSource;
    private readonly ChannelWriter<ChangeEvent<OrderModel>> _orderTarget;
    private readonly ChannelReader<TradeModel> _tradeSource;
    private readonly ChannelWriter<TradeModel> _tradeTarget;
    private readonly IExecutor _executor;
    private DisposableBox _sourceSubscriptions;

    protected UserConnectorBase(
        UserSettings settings,
        IUserProvider userProvider,
        IStatusMonitor monitor,
        ILogger logger
    )
    {
        Logger = logger;
        Id = $"{settings.Provider}[{settings.Environment}]{settings.Key[..7]}";
        Settings = settings;
        UserProvider = userProvider;

        Disposable = Annium.Disposable.AsyncBox(logger);

        Status = monitor.Status;

        monitor.OnStatusChanged += HandleStatusChanged;
        Disposable += () => monitor.OnStatusChanged -= HandleStatusChanged;

        monitor.OnError += HandleError;
        Disposable += () => monitor.OnError -= HandleError;

        // assets
        var assetSourceChannel = Channel.CreateUnbounded<ChangeEvent<AssetModel>>();
        AssetWriter = assetSourceChannel.Writer;
        _assetSource = assetSourceChannel.Reader;

        var assetTargetChannel = Channel.CreateUnbounded<ChangeEvent<AssetModel>>();
        _assetTarget = assetTargetChannel.Writer;
        Assets = assetTargetChannel.Reader.AsObservable().Publish().RefCount();

        // positions
        var positionSourceChannel = Channel.CreateUnbounded<ChangeEvent<PositionModel>>();
        PositionWriter = positionSourceChannel.Writer;
        _positionSource = positionSourceChannel.Reader;

        var positionTargetChannel = Channel.CreateUnbounded<ChangeEvent<PositionModel>>();
        _positionTarget = positionTargetChannel.Writer;
        Positions = positionTargetChannel.Reader.AsObservable().Publish().RefCount();

        // orders
        var orderSourceChannel = Channel.CreateUnbounded<ChangeEvent<OrderModel>>();
        OrderWriter = orderSourceChannel.Writer;
        _orderSource = orderSourceChannel.Reader;

        var orderTargetChannel = Channel.CreateUnbounded<ChangeEvent<OrderModel>>();
        _orderTarget = orderTargetChannel.Writer;
        Orders = orderTargetChannel.Reader.AsObservable().Publish().RefCount();

        // trades
        var tradeSourceChannel = Channel.CreateUnbounded<TradeModel>();
        TradeWriter = tradeSourceChannel.Writer;
        _tradeSource = tradeSourceChannel.Reader;

        var tradeTargetChannel = Channel.CreateUnbounded<TradeModel>();
        _tradeTarget = tradeTargetChannel.Writer;
        Trades = tradeTargetChannel.Reader.AsObservable().Publish().RefCount();

        Disposable += _executor = Executor.Sequential<MarketConnectorBase>(logger).Start();

        Disposable += _sourceSubscriptions = Annium.Disposable.Box(logger);
    }

    public ValueTask DisposeAsync()
    {
        return Disposable.DisposeAsync();
    }

    public void Sync()
    {
        this.Trace<string>("{id} schedule sync", Id);
        var scheduled = _executor.TrySchedule(async () =>
        {
            this.Trace<string>("{id} unsubscribe readers", Id);
            UnsubscribeReaders();

            this.Trace<string>("{id} sync start", Id);
            await OnSync(Settings, UserProvider);
            this.Trace<string>("{id} sync done", Id);

            this.Trace<string>("{id} subscribe readers", Id);
            SubscribeReaders();

            this.Trace("{id} notify {status} status", Id, ConnectorStatus.Connected);
            Status = ConnectorStatus.Connected;
            OnStatusChanged(ConnectorStatus.Connected);

            this.Trace<string>("{id} done sync", Id);
        });

        this.Trace("{id} done, result: {result}", Id, scheduled);
    }

    private void HandleStatusChanged(ConnectorStatus status)
    {
        if (status is not ConnectorStatus.Connected)
        {
            this.Trace("{id} notify {status} status", Id, status);
            Status = status;
            OnStatusChanged(status);
            return;
        }

        Sync();
    }

    private void HandleError(ConnectorError error)
    {
        OnError(error);
    }

    private void SubscribeReaders()
    {
        _sourceSubscriptions += _assetSource.Pipe(_assetTarget);
        _sourceSubscriptions += _positionSource.Pipe(_positionTarget);
        _sourceSubscriptions += _orderSource.Pipe(_orderTarget);
        _sourceSubscriptions += _tradeSource.Pipe(_tradeTarget);
    }

    private void UnsubscribeReaders()
    {
        _sourceSubscriptions.DisposeAndReset();
    }
}
