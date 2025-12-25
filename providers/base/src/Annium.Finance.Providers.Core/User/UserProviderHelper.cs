using System.Collections.Generic;
using Annium.Finance.Providers.Abstractions.Domain.User;
using NodaTime;

namespace Annium.Finance.Providers.Core.User;

public static class UserProviderHelper
{
    public static (Instant min, Instant max) ResolveHistoryBounds(Instant since, Instant now)
    {
        var min = since - Duration.FromSeconds(10);
        var max = now + Duration.FromMilliseconds(50);

        return (min, max);
    }

    public static void MergeOrders(Dictionary<string, OrderModel> orders, IReadOnlyCollection<OrderModel> models)
    {
        foreach (var model in models)
            orders.TryAdd(model.Id, model);
    }

    public static void MergeTrades(Dictionary<string, TradeModel> trades, IReadOnlyCollection<TradeModel> models)
    {
        foreach (var model in models)
            trades.TryAdd(model.Id, model);
    }
}
