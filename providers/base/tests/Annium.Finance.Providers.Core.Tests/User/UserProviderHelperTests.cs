using System.Collections.Generic;
using Annium.Finance.Providers.Abstractions.Domain.User;
using Annium.Finance.Providers.Core.User;
using Annium.Testing;
using NodaTime;
using Xunit;

namespace Annium.Finance.Providers.Core.Tests.User;

/// <summary>
/// Pins how a provider assembles order and trade history: the bounds it actually queries, and what happens
/// when the same record arrives from two fetches. Providers page history by time window and then by cursor
/// over the same ids, so both questions are asked on every history load, and neither had a test.
/// </summary>
public class UserProviderHelperTests
{
    /// <summary>An arbitrary fixed moment; only the differences from it matter.</summary>
    private static readonly Instant _now = Instant.FromUnixTimeMilliseconds(1_700_000_000_000);

    /// <summary>
    /// The queried range is wider than the one asked for, at both ends. This process and the exchange do not
    /// share a clock, so a record stamped a moment outside the literal window still belongs inside it —
    /// and querying the literal window drops it, leaving a history that looks complete and is not.
    /// </summary>
    [Fact]
    public void HistoryBounds_ArePaddedAgainstClockSkew()
    {
        // arrange
        var since = _now - Duration.FromHours(1);

        // act
        var (min, max) = UserProviderHelper.ResolveHistoryBounds(since, _now);

        // assert
        (min < since).IsTrue("the lower bound must reach back before the moment asked for");
        (max > _now).IsTrue("the upper bound must reach past the moment asked for");
        (since - min).Is(Duration.FromSeconds(10));
        (max - _now).Is(Duration.FromMilliseconds(50));
    }

    /// <summary>
    /// Merging keeps the record already held. The same order comes back from more than one fetch as history
    /// is paged, and the one already in hand came from the earlier, more complete read — letting a later
    /// fetch overwrite it would replace a record with a copy of itself at best, and with a partial one from
    /// a differently-scoped query at worst.
    /// </summary>
    [Fact]
    public void MergingOrders_KeepsTheOneAlreadyHeld()
    {
        // arrange
        var held = Order("1", OrderStatus.Filled);
        var orders = new Dictionary<string, OrderModel> { ["1"] = held };

        // act - the same id arrives again, in a different state, alongside one not seen before
        UserProviderHelper.MergeOrders(orders, [Order("1", OrderStatus.New), Order("2", OrderStatus.New)]);

        // assert
        orders.Count.Is(2);
        orders["1"].Status.Is(OrderStatus.Filled, "the order already held must not be replaced by a later fetch");
        orders["2"].Status.Is(OrderStatus.New, "an order not yet seen must be added");
    }

    /// <summary>
    /// The same for trades, which page the same way.
    /// </summary>
    [Fact]
    public void MergingTrades_KeepsTheOneAlreadyHeld()
    {
        // arrange
        var trades = new Dictionary<string, TradeModel> { ["1"] = Trade("1", 10m) };

        // act
        UserProviderHelper.MergeTrades(trades, [Trade("1", 20m), Trade("2", 30m)]);

        // assert
        trades.Count.Is(2);
        trades["1"].Price.Is(10m, "the trade already held must not be replaced by a later fetch");
        trades["2"].Price.Is(30m);
    }

    /// <summary>Builds an order carrying the given id and status; every other term is irrelevant here.</summary>
    /// <param name="id">The order's provider-assigned id.</param>
    /// <param name="status">The order's status.</param>
    /// <returns>The order.</returns>
    private static OrderModel Order(string id, OrderStatus status) =>
        new(
            id,
            "client-id",
            OrientationRange.Both,
            "BTCUSDT",
            OrderSide.Buy,
            OrderType.Limit,
            1m,
            10m,
            0m,
            false,
            0L,
            status,
            0m,
            0m,
            0L
        );

    /// <summary>Builds a trade carrying the given id and price; every other term is irrelevant here.</summary>
    /// <param name="id">The trade's provider-assigned id.</param>
    /// <param name="price">The price the trade executed at.</param>
    /// <returns>The trade.</returns>
    private static TradeModel Trade(string id, decimal price) =>
        new(id, "order-id", "BTCUSDT", 1m, price, "USDT", 0m, true, 0L);
}
