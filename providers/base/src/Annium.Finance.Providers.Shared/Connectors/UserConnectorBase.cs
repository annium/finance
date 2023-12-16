using System;
using System.Reactive.Linq;
using System.Threading.Channels;
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

public abstract class UserConnectorBase : IAsyncDisposable, ILogSubject
{
    public ConnectorStatus Status { get; private set; } = ConnectorStatus.Disconnected;
    public event Action<ConnectorStatus> OnStatusChanged = delegate { };
    public event Action<ConnectorError> OnError = delegate { };
    public ILogger Logger { get; }
    public ITableView<AssetDto> Assets => _assets;
    public ITableView<PositionDto> Positions => _positions;
    public ITableView<OrderDto> Orders => _orders;
    protected readonly ChannelWriter<AssetDto> AssetWriter;
    protected readonly ChannelWriter<PositionDto> PositionWriter;
    protected readonly ChannelWriter<OrderDto> OrderWriter;
    protected AsyncDisposableBox Disposable;
    private readonly ITable<AssetDto> _assets;
    private readonly ITable<PositionDto> _positions;
    private readonly ITable<OrderDto> _orders;
    private readonly ChannelReader<AssetDto> _assetReader;
    private readonly ChannelReader<PositionDto> _positionReader;
    private readonly ChannelReader<OrderDto> _orderReader;
    private readonly IExecutor _executor;
    private readonly IUserSynchronizer _synchronizer;
    private readonly UserSettings _config;
    private readonly IUserProvider _userProvider;
    private DisposableBox _sourceSubscriptions;

    protected UserConnectorBase(
        UserSettings config,
        IUserProvider userProvider,
        ITableFactory tableFactory,
        IStatusMonitor monitor,
        IUserSynchronizer synchronizer,
        ILogger logger
    )
    {
        Logger = logger;
        _config = config;
        _userProvider = userProvider;
        _synchronizer = synchronizer;

        Disposable = Annium.Disposable.AsyncBox(logger);

        monitor.OnStatusChanged += HandleStatusChanged;
        Disposable += () => monitor.OnStatusChanged -= HandleStatusChanged;

        //
        Disposable += _assets = tableFactory
            .New<AssetDto>()
            .Allow(TablePermission.All)
            .Key(x => x.Resource)
            .UpdateWith(AssetHasChanged, AssetUpdate)
            .Build();
        Disposable += _positions = tableFactory
            .New<PositionDto>()
            .Allow(TablePermission.All)
            .Key(x => new { x.Symbol, x.OrientationRange })
            .UpdateWith(PositionHasChanged, PositionUpdate)
            .Build();
        Disposable += _orders = tableFactory
            .New<OrderDto>()
            .Allow(TablePermission.All)
            .Key(x => x.Id)
            .UpdateWith(OrderHasChanged, OrderUpdate)
            .Keep(x => x.Status is OrderStatus.New or OrderStatus.PartiallyFilled or OrderStatus.Filled)
            .Build();

        Disposable += _executor = Executor.Sequential<MarketConnectorBase>(logger).Start();

        Disposable += _sourceSubscriptions = Annium.Disposable.Box(logger);

        var assetChannel = Channel.CreateUnbounded<AssetDto>();
        AssetWriter = assetChannel.Writer;
        _assetReader = assetChannel.Reader;

        var positionChannel = Channel.CreateUnbounded<PositionDto>();
        PositionWriter = positionChannel.Writer;
        _positionReader = positionChannel.Reader;

        var orderChannel = Channel.CreateUnbounded<OrderDto>();
        OrderWriter = orderChannel.Writer;
        _orderReader = orderChannel.Reader;
    }

    public ValueTask DisposeAsync()
    {
        return Disposable.DisposeAsync();
    }

    protected void ReportError(ConnectorError error) => OnError(error);

    private bool AssetHasChanged(AssetDto a, AssetDto b)
    {
        return a.Free != b.Free || a.Locked != b.Locked;
    }

    private void AssetUpdate(AssetDto source, AssetDto value)
    {
        source.Update(value.Free, value.Locked);
    }

    private bool PositionHasChanged(PositionDto a, PositionDto b)
    {
        return a.MarginType != b.MarginType || a.Leverage != b.Leverage || a.Amount != b.Amount;
    }

    private void PositionUpdate(PositionDto source, PositionDto value)
    {
        source.Update(value.MarginType, value.Leverage, value.Amount);
    }

    private bool OrderHasChanged(OrderDto a, OrderDto b)
    {
        return a.Side != b.Side
            || a.Type != b.Type
            || a.TotalQty != b.TotalQty
            || a.Price != b.Price
            || a.LevelPrice != b.LevelPrice
            || a.CreatedAt != b.CreatedAt
            || a.Status != b.Status
            || a.ExecutedQty != b.ExecutedQty
            || a.ExecutedPrice != b.ExecutedPrice
            || a.Fee != b.Fee
            || a.UpdatedAt != b.UpdatedAt;
    }

    private void OrderUpdate(OrderDto source, OrderDto value)
    {
        source.Update(
            value.Side,
            value.Type,
            value.TotalQty,
            value.Price,
            value.LevelPrice,
            value.CreatedAt,
            value.Status,
            value.ExecutedQty,
            value.ExecutedPrice,
            value.Fee,
            value.UpdatedAt
        );
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

        this.Trace("unsubscribe readers");
        UnsubscribeReaders();

        this.Trace("schedule sync");
        var scheduled = _executor.TrySchedule(async () =>
        {
            this.Trace("start sync");
            await _synchronizer.ExecuteAsync(_config, _userProvider, _assets, _positions, _orders);

            this.Trace("subscribe readers");
            SubscribeReaders();

            this.Trace("notify {status} status", status);
            Status = status;
            OnStatusChanged(status);

            this.Trace("done sync");
        });

        this.Trace("done, result: {result}", scheduled);
    }

    private void SubscribeReaders()
    {
        _sourceSubscriptions += _assetReader.AsObservable().Subscribe(_assets.Set);
        _sourceSubscriptions += _positionReader.AsObservable().Subscribe(_positions.Set);
        _sourceSubscriptions += _orderReader.AsObservable().Subscribe(_orders.Set);
    }

    private void UnsubscribeReaders()
    {
        _sourceSubscriptions.DisposeAndReset();
    }
}
