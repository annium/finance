using System.Collections.Generic;
using Annium.Finance.Providers.Abstractions.Domain.Models;
using Annium.Logging;
using NodaTime;

namespace Annium.Finance.Providers.Shared.Connectors;

public abstract class UserProviderBase : ILogSubject
{
    public ILogger Logger { get; }
    private readonly ITimeProvider _timeProvider;

    protected UserProviderBase(ITimeProvider timeProvider, ILogger logger)
    {
        Logger = logger;
        _timeProvider = timeProvider;
    }

    protected (Instant min, Instant max) ResolveHistoryBounds(long since)
    {
        var now = _timeProvider.Now;
        var instant = Instant.FromUnixTimeMilliseconds(since);

        var min = instant - Duration.FromSeconds(10);
        var max = now + Duration.FromMilliseconds(50);

        return (min, max);
    }

    protected void ResolveOrders(Dictionary<string, OrderModel> orders, IReadOnlyCollection<OrderModel> models)
    {
        foreach (var model in models)
            orders.TryAdd(model.Id, model);
    }

    protected Dictionary<string, OrderModel> ResolveOrders(IReadOnlyCollection<OrderModel> models)
    {
        var orders = new Dictionary<string, OrderModel>();

        ResolveOrders(orders, models);

        return orders;
    }

    protected void ResolveTrades(Dictionary<string, TradeModel> trades, IReadOnlyCollection<TradeModel> models)
    {
        foreach (var model in models)
            trades.TryAdd(model.Id, model);
    }

    protected Dictionary<string, TradeModel> ResolveTrades(IReadOnlyCollection<TradeModel> models)
    {
        var trades = new Dictionary<string, TradeModel>();

        ResolveTrades(trades, models);

        return trades;
    }
}
