using Annium.Finance.Providers.Abstractions.Domain.User;
using Annium.Finance.Providers.Abstractions.Domain.User.Requests;
using Annium.Testing;
using Xunit;
using static Annium.Finance.Providers.Abstractions.Domain.User.Requests.RequestBuilder;

namespace Annium.Finance.Providers.Abstractions.Domain.Tests.User.Requests;

/// <summary>
/// Pins which price each order type carries. <see cref="RequestBuilder"/> is where an order's type decides
/// whether it is priced by a limit price, by a trigger price, or by neither, and every request a connector
/// sends to an exchange is built here - so a limit price landing in the trigger field, or a trigger price in
/// the limit field, is an order placed at a price nobody asked for.
/// </summary>
public class RequestBuilderTests
{
    /// <summary>The order the modify requests in these tests are built against.</summary>
    private static readonly OrderModel _order = new(
        "id",
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
        OrderStatus.New,
        0m,
        0m,
        0L
    );

    /// <summary>
    /// Verifies that each order-init factory produces its own type, carries the terms it was given, and
    /// zeroes the price its type does not use.
    /// </summary>
    [Fact]
    public void InitRequests_CarryThePriceTheirTypeUses()
    {
        // assert - priced by a limit price only
        var limit = InitLimitOrder("id", OrientationRange.Both, "BTCUSDT", OrderSide.Buy, 2m, 10m);
        limit.Type.Is(OrderType.Limit);
        limit.Id.Is("id");
        limit.Symbol.Is("BTCUSDT");
        limit.Side.Is(OrderSide.Buy);
        limit.Qty.Is(2m);
        limit.Price.Is(10m);
        limit.LevelPrice.Is(0m);
        limit.ReduceOnly.IsFalse();

        // assert - priced by neither
        var market = InitMarketOrder("id", OrientationRange.Both, "BTCUSDT", OrderSide.Sell, 2m, true);
        market.Type.Is(OrderType.Market);
        market.Price.Is(0m);
        market.LevelPrice.Is(0m);
        market.ReduceOnly.IsTrue();

        // assert - priced by a trigger price only
        var stopMarket = InitStopLossMarketOrder("id", OrientationRange.Both, "BTCUSDT", OrderSide.Sell, 2m, 9m);
        stopMarket.Type.Is(OrderType.StopLossMarket);
        stopMarket.Price.Is(0m, "a stop-loss market order has no limit price");
        stopMarket.LevelPrice.Is(9m);

        var takeMarket = InitTakeProfitMarketOrder("id", OrientationRange.Both, "BTCUSDT", OrderSide.Sell, 2m, 11m);
        takeMarket.Type.Is(OrderType.TakeProfitMarket);
        takeMarket.Price.Is(0m, "a take-profit market order has no limit price");
        takeMarket.LevelPrice.Is(11m);

        // assert - priced by both, and the two must not be interchanged
        var stopLimit = InitStopLossLimitOrder("id", OrientationRange.Both, "BTCUSDT", OrderSide.Sell, 2m, 8m, 9m);
        stopLimit.Type.Is(OrderType.StopLossLimit);
        stopLimit.Price.Is(8m, "the limit price is what the order executes at");
        stopLimit.LevelPrice.Is(9m, "the level price is what triggers it");

        var takeLimit = InitTakeProfitLimitOrder("id", OrientationRange.Both, "BTCUSDT", OrderSide.Sell, 2m, 12m, 11m);
        takeLimit.Type.Is(OrderType.TakeProfitLimit);
        takeLimit.Price.Is(12m);
        takeLimit.LevelPrice.Is(11m);
    }

    /// <summary>
    /// Verifies that each modify factory keeps the order it modifies and applies the same per-type price
    /// rules as the matching init factory.
    /// </summary>
    [Fact]
    public void ModifyRequests_CarryThePriceTheirTypeUses()
    {
        // assert - priced by a limit price only
        var limit = ModifyToLimitOrder(_order, OrderSide.Buy, 2m, 10m);
        limit.Order.Is(_order);
        limit.Type.Is(OrderType.Limit);
        limit.Side.Is(OrderSide.Buy);
        limit.Qty.Is(2m);
        limit.Price.Is(10m);
        limit.LevelPrice.Is(0m);

        // assert - priced by neither
        var market = ModifyToMarketOrder(_order, OrderSide.Sell, 2m);
        market.Type.Is(OrderType.Market);
        market.Price.Is(0m);
        market.LevelPrice.Is(0m);

        // assert - priced by a trigger price only
        var stopMarket = ModifyToStopLossMarketOrder(_order, OrderSide.Sell, 2m, 9m);
        stopMarket.Type.Is(OrderType.StopLossMarket);
        stopMarket.Price.Is(0m, "a stop-loss market order has no limit price");
        stopMarket.LevelPrice.Is(9m);

        var takeMarket = ModifyToTakeProfitMarketOrder(_order, OrderSide.Sell, 2m, 11m);
        takeMarket.Type.Is(OrderType.TakeProfitMarket);
        takeMarket.Price.Is(0m, "a take-profit market order has no limit price");
        takeMarket.LevelPrice.Is(11m);

        // assert - priced by both, and the two must not be interchanged
        var stopLimit = ModifyToStopLossLimitOrder(_order, OrderSide.Sell, 2m, 8m, 9m);
        stopLimit.Type.Is(OrderType.StopLossLimit);
        stopLimit.Price.Is(8m);
        stopLimit.LevelPrice.Is(9m);

        var takeLimit = ModifyToTakeProfitLimitOrder(_order, OrderSide.Sell, 2m, 12m, 11m);
        takeLimit.Type.Is(OrderType.TakeProfitLimit);
        takeLimit.Price.Is(12m);
        takeLimit.LevelPrice.Is(11m);
    }

    /// <summary>
    /// Verifies that a cancellation carries both identifiers an exchange may key on, plus the symbol.
    /// </summary>
    [Fact]
    public void CancelRequest_CarriesBothIdentifiers()
    {
        // act
        var request = CancelOrder("id", "client-id", "BTCUSDT");

        // assert
        request.Id.Is("id");
        request.ClientOrderId.Is("client-id");
        request.Symbol.Is("BTCUSDT");
    }
}
