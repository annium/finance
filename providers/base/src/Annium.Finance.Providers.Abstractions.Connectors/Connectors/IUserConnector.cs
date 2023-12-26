using System;
using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Domain.Dto;
using Annium.Finance.Providers.Abstractions.Domain.Interfaces;
using Annium.Finance.Providers.Abstractions.Domain.Operations;

namespace Annium.Finance.Providers.Abstractions.Connectors.Connectors;

public interface IUserConnector : IConnectorBase
{
    IObservable<AssetDto> Assets { get; }
    IObservable<PositionDto> Positions { get; }
    IObservable<OrderDto> Orders { get; }
    IObservable<TradeDto> Trades { get; }
    Task<UserResult> SetLeverage(PositionDto position, decimal leverage);
    Task<UserResult<OrderDto>> InitOrder(IInitOrderRequest request);
    Task<UserResult<OrderDto>> ModifyOrder(IModifyOrderRequest request);
    Task<UserResult> CancelOrder(OrderDto order);
    Task<UserResult> CancelAllOrders(string symbol);
}
