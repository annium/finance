using System;
using System.Threading.Tasks;
using Annium.Data.Tables;
using Annium.Finance.Providers.Abstractions.Domain.Interfaces;
using Annium.Finance.Providers.Abstractions.Domain.Models;
using Annium.Finance.Providers.Abstractions.Domain.Operations;

namespace Annium.Finance.Providers.Abstractions.Connectors.Connectors;

public interface IUserConnector : IConnectorBase
{
    IObservable<ChangeEvent<AssetModel>> Assets { get; }
    IObservable<ChangeEvent<PositionModel>> Positions { get; }
    IObservable<ChangeEvent<OrderModel>> Orders { get; }
    IObservable<TradeModel> Trades { get; }
    event Func<UserSettings, IUserProvider, Task> OnSync;
    Task<UserResult> SetLeverage(PositionModel position, decimal leverage);
    Task<UserResult<OrderModel?>> InitOrder(IInitOrderRequest request);
    Task<UserResult<OrderModel?>> ModifyOrder(IModifyOrderRequest request);
    Task<UserResult> CancelOrder(OrderModel order);
    Task<UserResult> CancelAllOrders(string symbol);
}
