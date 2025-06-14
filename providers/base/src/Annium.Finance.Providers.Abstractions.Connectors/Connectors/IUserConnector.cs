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
    void Sync();
    Task<UserResult> SetLeverageAsync(PositionModel position, decimal leverage);
    Task<UserResult<OrderModel?>> InitOrderAsync(IInitOrderRequest request);
    Task<UserResult<OrderModel?>> ModifyOrderAsync(IModifyOrderRequest request);
    Task<UserResult> CancelOrderAsync(ICancelOrderRequest request);
    Task<UserResult> CancelAllOrdersAsync(string symbol);
}
