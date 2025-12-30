using System;
using System.Threading.Tasks;
using Annium.Data.Tables;
using Annium.Finance.Providers.Abstractions.Connectors.Shared;
using Annium.Finance.Providers.Abstractions.Domain.User;
using Annium.Finance.Providers.Abstractions.Domain.User.Operations;
using Annium.Finance.Providers.Abstractions.Domain.User.Requests;

namespace Annium.Finance.Providers.Abstractions.Connectors.User;

public interface IUserConnector : IConnectorBase
{
    IObservable<ChangeEvent<AssetModel>> Assets { get; }
    IObservable<ChangeEvent<PositionModel>> Positions { get; }
    IObservable<ChangeEvent<OrderModel>> Orders { get; }
    IObservable<TradeModel> Trades { get; }
    event Func<UserSettings, IUserProvider, Task> OnSync;
    void Sync();
    Task<UserResult> SetLeverageAsync(PositionModel position, decimal leverage);
    Task<UserResult<OrderModel?>> InitOrderAsync(IInitOrderRequest request);
    Task<UserResult<OrderModel?>> ModifyOrderAsync(IModifyOrderRequest request);
    Task<UserResult> CancelOrderAsync(ICancelOrderRequest request);
    Task<UserResult> CancelAllOrdersAsync(string symbol);
}

public interface IUserConnectorNew : IConnectorBase
{
    IObservable<AssetModel> Assets { get; }
    IObservable<UserResult<PositionModel>> Positions { get; }
    IObservable<UserResult<OrderModel>> Orders { get; }
    IObservable<TradeModel> Trades { get; }
    event Func<UserSettings, IUserProvider, Task> OnSync;
    void Sync();
    // leverage update is returned as UserResult<PositionModel> events
    void SetLeverage(PositionModel position, decimal leverage);
    // order updates are returned as UserResult<OrderModel> events
    void InitOrder(IInitOrderRequest request);
    void ModifyOrder(IModifyOrderRequest request);
    void CancelOrder(ICancelOrderRequest request);
    void CancelAllOrders(string symbol);
}
