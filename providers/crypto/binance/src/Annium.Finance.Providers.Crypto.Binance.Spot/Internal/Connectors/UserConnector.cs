using System.Threading.Tasks;
using Annium.Data.Tables;
using Annium.Finance.Providers.Abstractions.Connectors.Connectors;
using Annium.Finance.Providers.Abstractions.Domain.Dto;
using Annium.Finance.Providers.Abstractions.Domain.Interfaces;
using Annium.Finance.Providers.Abstractions.Domain.Operations;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Internal.Connectors;

internal class UserConnector : IUserConnector
{
    public IUserConfig Config { get; }

    public ValueTask InitAsync(IUserConfig config)
    {
        throw new System.NotImplementedException();
    }

    public ValueTask DisposeAsync()
    {
        throw new System.NotImplementedException();
    }

    public ITableView<AssetDto> Assets { get; }
    public ITableView<PositionDto> Positions { get; }
    public ITableView<OrderDto> Orders { get; }

    public Task<Result<UserOperationStatus>> SetLeverage(PositionDto position, byte leverage)
    {
        throw new System.NotImplementedException();
    }

    public Task<Result<UserOperationStatus, OrderDto>> InitOrder(IInitOrderRequest order)
    {
        throw new System.NotImplementedException();
    }

    public Task<Result<UserOperationStatus, OrderDto>> ModifyOrder(IModifyOrderRequest order)
    {
        throw new System.NotImplementedException();
    }

    public Task<Result<UserOperationStatus>> CancelOrder(OrderDto order)
    {
        throw new System.NotImplementedException();
    }

    public Task<Result<UserOperationStatus>> CancelAllOrders(string symbol)
    {
        throw new System.NotImplementedException();
    }
}
