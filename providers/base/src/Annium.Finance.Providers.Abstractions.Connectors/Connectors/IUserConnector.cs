using System;
using System.Threading.Tasks;
using Annium.Data.Tables;
using Annium.Finance.Providers.Abstractions.Domain.Dto;
using Annium.Finance.Providers.Abstractions.Domain.Interfaces;
using Annium.Finance.Providers.Abstractions.Domain.Operations;

namespace Annium.Finance.Providers.Abstractions.Connectors.Connectors;

public interface IUserConnector : IConnectorBase<IUserConfig>, IAsyncDisposable
{
    ITableView<AssetDto> Assets { get; }
    ITableView<PositionDto> Positions { get; }
    ITableView<OrderDto> Orders { get; }
    Task<Result<UserOperationStatus>> SetLeverage(PositionDto position, byte leverage);
    Task<Result<UserOperationStatus, OrderDto>> InitOrder(IInitOrderRequest order);
    Task<Result<UserOperationStatus, OrderDto>> ModifyOrder(IModifyOrderRequest order);
    Task<Result<UserOperationStatus>> CancelOrder(OrderDto order);
    Task<Result<UserOperationStatus>> CancelAllOrders(string symbol);
}
