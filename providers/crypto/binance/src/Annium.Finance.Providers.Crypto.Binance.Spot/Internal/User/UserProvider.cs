using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Connectors.User;
using Annium.Finance.Providers.Abstractions.Domain.User;
using Annium.Finance.Providers.Abstractions.Domain.User.Operations;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Internal.User;

internal class UserProvider : IUserProvider
{
    public Task<UserResult<UserContext?>> LoadContextAsync()
    {
        var assets = Array.Empty<AssetModel>();
        var positions = Array.Empty<PositionModel>();

        var result = UserResult.Ok<UserContext?>(new UserContext(assets, positions));

        return Task.FromResult(result);
    }

    public Task<UserResult<IReadOnlyCollection<OrderModel>?>> LoadOpenOrdersAsync()
    {
        var orders = Array.Empty<OrderModel>();

        var result = UserResult.Ok<IReadOnlyCollection<OrderModel>?>(orders);

        return Task.FromResult(result);
    }

    public Task<UserResult<IReadOnlyCollection<OrderModel>?>> LoadOrdersAsync(string symbol, long? since)
    {
        var orders = Array.Empty<OrderModel>();

        var result = UserResult.Ok<IReadOnlyCollection<OrderModel>?>(orders);

        return Task.FromResult(result);
    }

    public Task<UserResult<IReadOnlyCollection<TradeModel>?>> LoadTradesAsync(string symbol, long? since)
    {
        var trades = Array.Empty<TradeModel>();

        var result = UserResult.Ok<IReadOnlyCollection<TradeModel>?>(trades);

        return Task.FromResult(result);
    }
}
