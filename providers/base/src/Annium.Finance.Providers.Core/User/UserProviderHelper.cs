using System.Collections.Generic;
using Annium.Finance.Providers.Abstractions.Domain.User;
using NodaTime;

namespace Annium.Finance.Providers.Core.User;

/// <summary>
/// Helper methods shared by <see cref="Annium.Finance.Providers.Abstractions.Connectors.User.IUserProvider"/>
/// implementations for loading and merging order/trade history.
/// </summary>
public static class UserProviderHelper
{
    /// <summary>
    /// Widens a requested history range into safe bounds for a history query, padding the lower bound backwards
    /// and the upper bound forwards to tolerate clock skew between this process and the provider.
    /// </summary>
    /// <param name="since">The requested start of the history range.</param>
    /// <param name="now">The requested end of the history range.</param>
    /// <returns>The widened <c>min</c>/<c>max</c> bounds to query the provider with.</returns>
    public static (Instant min, Instant max) ResolveHistoryBounds(Instant since, Instant now)
    {
        var min = since - Duration.FromSeconds(10);
        var max = now + Duration.FromMilliseconds(50);

        return (min, max);
    }

    /// <summary>
    /// Merges newly loaded orders into an existing dictionary, keyed by id, without overwriting orders already
    /// present.
    /// </summary>
    /// <param name="orders">The dictionary to merge into.</param>
    /// <param name="models">The newly loaded orders.</param>
    public static void MergeOrders(Dictionary<string, OrderModel> orders, IReadOnlyCollection<OrderModel> models)
    {
        foreach (var model in models)
            orders.TryAdd(model.Id, model);
    }

    /// <summary>
    /// Merges newly loaded trades into an existing dictionary, keyed by id, without overwriting trades already
    /// present.
    /// </summary>
    /// <param name="trades">The dictionary to merge into.</param>
    /// <param name="models">The newly loaded trades.</param>
    public static void MergeTrades(Dictionary<string, TradeModel> trades, IReadOnlyCollection<TradeModel> models)
    {
        foreach (var model in models)
            trades.TryAdd(model.Id, model);
    }
}
