using System;
using System.Threading.Tasks;
using Annium.Data.Tables;
using Annium.Finance.Providers.Abstractions.Domain.Dto;
using Annium.Finance.Providers.Abstractions.Domain.Interfaces;
using Annium.Finance.Providers.Abstractions.Domain.Operations;

namespace Annium.Finance.Providers.Abstractions.Connectors.Connectors;

public interface IUserConnector : IConnectorBase
{
    event Action<ConnectorError> OnError;
    ITableView<AssetDto> Assets { get; }
    ITableView<PositionDto> Positions { get; }
    ITableView<OrderDto> Orders { get; }
    Task<UserResult> SetLeverage(PositionDto position, byte leverage);
    Task<UserResult<OrderDto>> InitOrder(IInitOrderRequest order);
    Task<UserResult<OrderDto>> ModifyOrder(IModifyOrderRequest order);
    Task<UserResult> CancelOrder(OrderDto order);
    Task<UserResult> CancelAllOrders(string symbol);
}

public sealed record ConnectorError(UserOperationStatus Status, string Message);
