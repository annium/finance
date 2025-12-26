using System;
using System.Threading.Tasks;
using Annium.Data.Tables;
using Annium.Finance.Providers.Abstractions.Connectors.Shared;
using Annium.Finance.Providers.Abstractions.Connectors.User;
using Annium.Finance.Providers.Abstractions.Domain.User;
using Annium.Finance.Providers.Abstractions.Domain.User.Operations;
using Annium.Finance.Providers.Abstractions.Domain.User.Requests;
using Annium.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace Annium.Finance.Providers.Core.Internal.User;

internal class UserConnectorContainer : IUserConnector, ILogSubject
{
    public ILogger Logger { get; }
    public ConnectorStatus Status => _connector.Status;
    public IObservable<ChangeEvent<AssetModel>> Assets => _connector.Assets;
    public IObservable<ChangeEvent<PositionModel>> Positions => _connector.Positions;
    public IObservable<ChangeEvent<OrderModel>> Orders => _connector.Orders;
    public IObservable<TradeModel> Trades => _connector.Trades;

    public event Action<ConnectorStatus> OnStatusChanged = delegate { };
    public event Action<ConnectorError> OnError = delegate { };
    public event Func<UserSettings, IUserProvider, Task> OnSync = delegate
    {
        return Task.CompletedTask;
    };

    private readonly AsyncServiceScope _scope;
    private readonly IUserConnector _connector;
    private readonly DisposableBox _disposable;

    public UserConnectorContainer(AsyncServiceScope scope, IUserConnector connector, ILogger logger)
    {
        Logger = logger;
        _scope = scope;
        _connector = connector;
        _disposable = Disposable.Box(logger);

        _connector.OnStatusChanged += HandleStatusChanged;
        _disposable += () => _connector.OnStatusChanged -= HandleStatusChanged;

        _connector.OnError += HandleError;
        _disposable += () => _connector.OnError -= HandleError;

        _connector.OnSync += HandleSyncAsync;
        _disposable += () => _connector.OnSync -= HandleSyncAsync;
    }

    public async ValueTask DisposeAsync()
    {
        this.Trace("dispose connector bindings");
        _disposable.Dispose();

        this.Trace("dispose connector");
        await _connector.DisposeAsync();

        this.Trace("dispose scope");
        await _scope.DisposeAsync();

        this.Trace("done");
    }

    public void Sync()
    {
        _connector.Sync();
    }

    public Task<UserResult> SetLeverageAsync(PositionModel position, decimal leverage)
    {
        return _connector.SetLeverageAsync(position, leverage);
    }

    public Task<UserResult<OrderModel?>> InitOrderAsync(IInitOrderRequest request)
    {
        return _connector.InitOrderAsync(request);
    }

    public Task<UserResult<OrderModel?>> ModifyOrderAsync(IModifyOrderRequest request)
    {
        return _connector.ModifyOrderAsync(request);
    }

    public Task<UserResult> CancelOrderAsync(ICancelOrderRequest order)
    {
        return _connector.CancelOrderAsync(order);
    }

    public Task<UserResult> CancelAllOrdersAsync(string symbol)
    {
        return _connector.CancelAllOrdersAsync(symbol);
    }

    private void HandleStatusChanged(ConnectorStatus status) => OnStatusChanged(status);

    private void HandleError(ConnectorError error) => OnError(error);

    private Task HandleSyncAsync(UserSettings settings, IUserProvider provider) => OnSync(settings, provider);
}
