using System;
using System.Reactive.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;
using Annium.Execution.Background;
using Annium.Finance.Providers.Abstractions.Connectors.Connectors;
using Annium.Finance.Providers.Abstractions.Connectors.Sync;
using Annium.Finance.Providers.Abstractions.Domain.Dto;
using Annium.Finance.Providers.Abstractions.Domain.Models;
using Annium.Logging;
using Annium.Threading.Channels;

namespace Annium.Finance.Providers.Shared.Connectors;

public abstract class UserConnectorBase : IAsyncDisposable, ILogSubject
{
    public ILogger Logger { get; }
    public ConnectorStatus Status { get; private set; } = ConnectorStatus.Disconnected;
    public IObservable<AssetDto> Assets { get; }
    public IObservable<PositionDto> Positions { get; }
    public IObservable<OrderDto> Orders { get; }
    public IObservable<TradeDto> Trades { get; }
    public event Action<ConnectorStatus> OnStatusChanged = delegate { };
    public event Action<ConnectorError> OnError = delegate { };
    protected readonly ChannelWriter<AssetDto> AssetWriter;
    protected readonly ChannelWriter<PositionDto> PositionWriter;
    protected readonly ChannelWriter<OrderDto> OrderWriter;
    protected readonly ChannelWriter<TradeDto> TradeWriter;
    protected AsyncDisposableBox Disposable;
    private readonly ChannelReader<AssetDto> _assetSource;
    private readonly ChannelWriter<AssetDto> _assetTarget;
    private readonly ChannelReader<PositionDto> _positionSource;
    private readonly ChannelWriter<PositionDto> _positionTarget;
    private readonly ChannelReader<OrderDto> _orderSource;
    private readonly ChannelWriter<OrderDto> _orderTarget;
    private readonly ChannelReader<TradeDto> _tradeSource;
    private readonly ChannelWriter<TradeDto> _tradeTarget;
    private readonly IExecutor _executor;
    private readonly IUserSynchronizer _synchronizer;
    private readonly UserSettings _settings;
    private readonly IUserProvider _userProvider;
    private DisposableBox _sourceSubscriptions;

    protected UserConnectorBase(
        UserSettings settings,
        IUserProvider userProvider,
        IStatusMonitor monitor,
        IUserSynchronizer synchronizer,
        ILogger logger
    )
    {
        Logger = logger;
        _settings = settings;
        _userProvider = userProvider;
        _synchronizer = synchronizer;

        Disposable = Annium.Disposable.AsyncBox(logger);

        monitor.OnStatusChanged += HandleStatusChanged;
        Disposable += () => monitor.OnStatusChanged -= HandleStatusChanged;

        monitor.OnError += HandleError;
        Disposable += () => monitor.OnError -= HandleError;

        // assets
        var assetSourceChannel = Channel.CreateUnbounded<AssetDto>();
        AssetWriter = assetSourceChannel.Writer;
        _assetSource = assetSourceChannel.Reader;

        var assetTargetChannel = Channel.CreateUnbounded<AssetDto>();
        _assetTarget = assetTargetChannel.Writer;
        Assets = assetTargetChannel.Reader.AsObservable().Publish().RefCount();

        // positions
        var positionSourceChannel = Channel.CreateUnbounded<PositionDto>();
        PositionWriter = positionSourceChannel.Writer;
        _positionSource = positionSourceChannel.Reader;

        var positionTargetChannel = Channel.CreateUnbounded<PositionDto>();
        _positionTarget = positionTargetChannel.Writer;
        Positions = positionTargetChannel.Reader.AsObservable().Publish().RefCount();

        // orders
        var orderSourceChannel = Channel.CreateUnbounded<OrderDto>();
        OrderWriter = orderSourceChannel.Writer;
        _orderSource = orderSourceChannel.Reader;

        var orderTargetChannel = Channel.CreateUnbounded<OrderDto>();
        _orderTarget = orderTargetChannel.Writer;
        Orders = orderTargetChannel.Reader.AsObservable().Publish().RefCount();

        // trades
        var tradeSourceChannel = Channel.CreateUnbounded<TradeDto>();
        TradeWriter = tradeSourceChannel.Writer;
        _tradeSource = tradeSourceChannel.Reader;

        var tradeTargetChannel = Channel.CreateUnbounded<TradeDto>();
        _tradeTarget = tradeTargetChannel.Writer;
        Trades = tradeTargetChannel.Reader.AsObservable().Publish().RefCount();

        Disposable += _executor = Executor.Sequential<MarketConnectorBase>(logger).Start();

        Disposable += _sourceSubscriptions = Annium.Disposable.Box(logger);
    }

    public ValueTask DisposeAsync()
    {
        return Disposable.DisposeAsync();
    }

    private void HandleStatusChanged(ConnectorStatus status)
    {
        if (status is not ConnectorStatus.Connected)
        {
            this.Trace("notify {status} status", status);
            Status = status;
            OnStatusChanged(status);
            return;
        }

        this.Trace("schedule sync");
        var scheduled = _executor.TrySchedule(async () =>
        {
            this.Trace("unsubscribe readers");
            UnsubscribeReaders();

            this.Trace("sync start");
            await _synchronizer.ExecuteAsync(_settings, _userProvider);
            this.Trace("sync done");

            this.Trace("subscribe readers");
            SubscribeReaders();

            this.Trace("notify {status} status", status);
            Status = status;
            OnStatusChanged(status);

            this.Trace("done sync");
        });

        this.Trace("done, result: {result}", scheduled);
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
