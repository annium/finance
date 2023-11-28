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
    public event Action<ConnectorStatus> OnStatusChanged = delegate { };
    public event Action<ConnectorError> OnError = delegate { };
    public ITableView<AssetDto> Assets { get; } = default!;
    public ITableView<PositionDto> Positions { get; } = default!;
    public ITableView<OrderDto> Orders { get; } = default!;

    public ValueTask InitAsync()
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
