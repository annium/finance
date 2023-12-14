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

public abstract class UserConnectorBase : IAsyncDisposable, ILogSubject
{
    public event Action<ConnectorStatus> OnStatusChanged = delegate { };
    public event Action<ConnectorError> OnError = delegate { };
    public ILogger Logger { get; }
    public ITableView<AssetDto> Assets => AssetsTable;
    public ITableView<PositionDto> Positions => PositionsTable;
    public ITableView<OrderDto> Orders => OrdersTable;
    protected readonly ITable<AssetDto> AssetsTable;
    protected readonly ITable<PositionDto> PositionsTable;
    protected readonly ITable<OrderDto> OrdersTable;
    protected AsyncDisposableBox Disposable;
    private readonly IExecutor _executor;
    private readonly IUserSynchronizer _synchronizer;
    private readonly UserSettings _config;
    private readonly IUserProvider _userProvider;

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
        Disposable += AssetsTable = tableFactory
            .New<AssetDto>()
            .Allow(TablePermission.All)
            .Key(x => x.Resource)
            .UpdateWith(AssetHasChanged, AssetUpdate)
            .Build();
        Disposable += PositionsTable = tableFactory
            .New<PositionDto>()
            .Allow(TablePermission.All)
            .Key(x => new { x.Symbol, x.OrientationRange })
            .UpdateWith(PositionHasChanged, PositionUpdate)
            .Build();
        Disposable += OrdersTable = tableFactory
            .New<OrderDto>()
            .Allow(TablePermission.All)
            .Key(x => x.Id)
            .UpdateWith(OrderHasChanged, OrderUpdate)
            .Keep(x => x.Status is OrderStatus.New or OrderStatus.PartiallyFilled or OrderStatus.Filled)
            .Build();

        Disposable += _executor = Executor.Sequential<MarketConnectorBase>(logger).Start();
    }

    public ValueTask DisposeAsync()
    {
        return Disposable.DisposeAsync();
    }

    protected void ScheduleSync()
    {
        this.Trace("start");

        var scheduled = _executor.TrySchedule(async () =>
        {
            this.Trace("start sync");
            await _synchronizer.ExecuteAsync(_config, _userProvider, AssetsTable, PositionsTable, OrdersTable);
            this.Trace("done sync");
        });

        this.Trace("done, result: {result}", scheduled);
    }

    protected void ReportError(ConnectorError error) => OnError(error);

    private bool AssetHasChanged(AssetDto a, AssetDto b)
    {
        throw new NotImplementedException();
    }

    private void AssetUpdate(AssetDto a, AssetDto b)
    {
        throw new NotImplementedException();
    }

    private bool PositionHasChanged(PositionDto a, PositionDto b)
    {
        throw new NotImplementedException();
    }

    private void PositionUpdate(PositionDto a, PositionDto b)
    {
        throw new NotImplementedException();
    }

    private bool OrderHasChanged(OrderDto a, OrderDto b)
    {
        throw new NotImplementedException();
    }

    private void OrderUpdate(OrderDto a, OrderDto b)
    {
        throw new NotImplementedException();
    }

    private void HandleStatusChanged(ConnectorStatus status) => OnStatusChanged(status);
}
