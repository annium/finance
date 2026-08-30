using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Connectors.User;
using Annium.Finance.Providers.Abstractions.Domain.User;
using Annium.Finance.Providers.Abstractions.Domain.User.Operations;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Internal.User;

/// <summary>
/// Loads Binance spot account data. Not implemented yet: every load always succeeds with an empty result,
/// and positions are always empty since spot has none.
/// </summary>
internal class UserProvider : IUserProvider
{
    /// <summary>Not implemented yet; always returns an empty asset and position set.</summary>
    /// <returns>A successful result carrying an empty user context.</returns>
    public Task<UserResult<UserContext?>> LoadContextAsync()
    {
        var assets = Array.Empty<AssetModel>();
        var positions = Array.Empty<PositionModel>();

        var result = UserResult.Ok<UserContext?>(new UserContext(assets, positions));

        return Task.FromResult(result);
    }

    /// <summary>Not implemented yet; always returns an empty order set.</summary>
    /// <returns>A successful result carrying no open orders.</returns>
    public Task<UserResult<IReadOnlyCollection<OrderModel>?>> LoadOpenOrdersAsync()
    {
        var orders = Array.Empty<OrderModel>();

        var result = UserResult.Ok<IReadOnlyCollection<OrderModel>?>(orders);

        return Task.FromResult(result);
    }

    /// <summary>Not implemented yet; always returns an empty order set.</summary>
    /// <param name="symbol">The instrument symbol to load orders for.</param>
    /// <param name="since">The point in time to load orders from, if any.</param>
    /// <returns>A successful result carrying no orders.</returns>
    public Task<UserResult<IReadOnlyCollection<OrderModel>?>> LoadOrdersAsync(string symbol, long? since)
    {
        var orders = Array.Empty<OrderModel>();

        var result = UserResult.Ok<IReadOnlyCollection<OrderModel>?>(orders);

        return Task.FromResult(result);
    }

    /// <summary>Not implemented yet; always returns an empty trade set.</summary>
    /// <param name="symbol">The instrument symbol to load trades for.</param>
    /// <param name="since">The point in time to load trades from, if any.</param>
    /// <returns>A successful result carrying no trades.</returns>
    public Task<UserResult<IReadOnlyCollection<TradeModel>?>> LoadTradesAsync(string symbol, long? since)
    {
        var trades = Array.Empty<TradeModel>();

        var result = UserResult.Ok<IReadOnlyCollection<TradeModel>?>(trades);

        return Task.FromResult(result);
    }
}
