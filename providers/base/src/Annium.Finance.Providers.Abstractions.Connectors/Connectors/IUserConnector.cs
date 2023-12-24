using System;
using System.Threading.Tasks;
using Annium.Data.Tables;
using Annium.Finance.Providers.Abstractions.Domain.Dto;
using Annium.Finance.Providers.Abstractions.Domain.Interfaces;
using Annium.Finance.Providers.Abstractions.Domain.Operations;

namespace Annium.Finance.Providers.Abstractions.Connectors.Connectors;

public interface IUserConnector : IConnectorBase
{
    ITableView<AssetDto> Assets { get; }
    ITableView<PositionDto> Positions { get; }
    ITableView<OrderDto> Orders { get; }
    IObservable<TradeDto> Trades { get; }
    Task<UserResult> SetLeverage(PositionDto position, decimal leverage);
    Task<UserResult<OrderDto>> InitOrder(IInitOrderRequest request);
    Task<UserResult<OrderDto>> ModifyOrder(IModifyOrderRequest request);
    Task<UserResult> CancelOrder(OrderDto order);
    Task<UserResult> CancelAllOrders(string symbol);
}
