using System;
using System.Threading.Tasks;
using Annium.Data.Tables;
using Annium.Finance.Providers.Abstractions.Connectors.Connectors;
using Annium.Finance.Providers.Abstractions.Domain.Dto;
using Annium.Finance.Providers.Abstractions.Domain.Interfaces;
using Annium.Finance.Providers.Abstractions.Domain.Operations;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Connectors;

internal class UserConnector : IUserConnector
{
    public IUserConfig Config { get; }
    public ITableView<AssetDto> Assets { get; }
    public ITableView<PositionDto> Positions { get; }
    public ITableView<OrderDto> Orders { get; }

    public ValueTask InitAsync(IUserConfig config)
    {
        throw new NotImplementedException();
    }

    public ValueTask DisposeAsync()
    {
        throw new NotImplementedException();
    }

    public Task<UserResult> SetLeverage(PositionDto position, byte leverage)
    {
        throw new NotImplementedException();
    }

    public Task<UserResult<OrderDto>> InitOrder(IInitOrderRequest order)
    {
        throw new NotImplementedException();
    }

    public Task<UserResult<OrderDto>> ModifyOrder(IModifyOrderRequest order)
    {
        throw new NotImplementedException();
    }

    public Task<UserResult> CancelOrder(OrderDto order)
    {
        throw new NotImplementedException();
    }

    public Task<UserResult> CancelAllOrders(string symbol)
    {
        throw new NotImplementedException();
    }
}
