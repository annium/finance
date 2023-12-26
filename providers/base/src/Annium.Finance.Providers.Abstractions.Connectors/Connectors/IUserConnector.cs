using System;
using System.Threading.Tasks;
using Annium.Data.Tables;
using Annium.Finance.Providers.Abstractions.Domain.Dto;
using Annium.Finance.Providers.Abstractions.Domain.Interfaces;
using Annium.Finance.Providers.Abstractions.Domain.Models;
using Annium.Finance.Providers.Abstractions.Domain.Operations;

namespace Annium.Finance.Providers.Abstractions.Connectors.Connectors;

public interface IUserConnector : IConnectorBase
{
    IObservable<ChangeEvent<AssetDto>> Assets { get; }
    IObservable<ChangeEvent<PositionDto>> Positions { get; }
    IObservable<ChangeEvent<OrderDto>> Orders { get; }
    IObservable<TradeDto> Trades { get; }
    event Func<UserSettings, IUserProvider, Task> OnSync;
    Task<UserResult> SetLeverage(PositionDto position, decimal leverage);
    Task<UserResult<OrderDto>> InitOrder(IInitOrderRequest request);
    Task<UserResult<OrderDto>> ModifyOrder(IModifyOrderRequest request);
    Task<UserResult> CancelOrder(OrderDto order);
    Task<UserResult> CancelAllOrders(string symbol);
}
