using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Connectors.Connectors;
using Annium.Finance.Providers.Abstractions.Domain.Models;
using Annium.Finance.Providers.Abstractions.Domain.Operations;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Internal.Connectors;

internal class UserProvider : IUserProvider
{
    public Task<UserResult<UserContext?>> LoadContextAsync(UserSettings settings)
    {
        var assets = Array.Empty<AssetModel>();
        var positions = Array.Empty<PositionModel>();

        var result = UserResult.Ok<UserContext?>(new UserContext(assets, positions));

        return Task.FromResult(result);
    }

    public Task<UserResult<IReadOnlyCollection<OrderModel>?>> LoadOrdersAsync(
        UserSettings settings,
        IReadOnlyCollection<string> symbols,
        long? since
    )
    {
        var orders = Array.Empty<OrderModel>();

        var result = UserResult.Ok<IReadOnlyCollection<OrderModel>?>(orders);

        return Task.FromResult(result);
    }

    public Task<UserResult<IReadOnlyCollection<TradeModel>?>> LoadTradesAsync(
        UserSettings settings,
        IReadOnlyCollection<string> symbols,
        long since
    )
    {
        var trades = Array.Empty<TradeModel>();

        var result = UserResult.Ok<IReadOnlyCollection<TradeModel>?>(trades);

        return Task.FromResult(result);
    }
}
