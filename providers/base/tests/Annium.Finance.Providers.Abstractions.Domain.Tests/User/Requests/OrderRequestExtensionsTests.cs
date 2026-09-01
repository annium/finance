using Annium.Finance.Providers.Abstractions.Domain.User;
using Annium.Finance.Providers.Abstractions.Domain.User.Requests;
using Annium.Testing;
using Xunit;
using static Annium.Finance.Providers.Abstractions.Domain.User.Requests.RequestBuilder;

namespace Annium.Finance.Providers.Abstractions.Domain.Tests.User.Requests;

/// <summary>
/// Pins how a request is classified and priced before it is sent. <see cref="InitOrderRequestExtensions"/>
/// answers, for a request that no order exists for yet, the same questions <c>OrderExtensions</c> answers for
/// a placed one — which price the order is aimed at, and whether it fills at once or waits on a trigger — and
/// <see cref="ModifyOrderRequestExtensions.ToInitOrderRequest"/> turns a modification back into the request
/// that would produce the same order.
/// </summary>
public class OrderRequestExtensionsTests
{
    /// <summary>
    /// Verifies that each order type is classified as immediate or leveled, and limit or market, consistently
    /// with the price the corresponding factory fills in.
    /// </summary>
    /// <param name="type">The order type under test.</param>
    /// <param name="isImmediate">Whether the type fills as soon as it is accepted.</param>
    /// <param name="isLeveled">Whether the type waits on a trigger price.</param>
    /// <param name="isLimit">Whether the type carries a limit price.</param>
    /// <param name="isMarket">Whether the type executes at the market price.</param>
    [Theory]
    [InlineData(OrderType.Limit, true, false, true, false)]
    [InlineData(OrderType.Market, true, false, false, true)]
    [InlineData(OrderType.StopLossMarket, false, true, false, true)]
    [InlineData(OrderType.TakeProfitMarket, false, true, false, true)]
    [InlineData(OrderType.StopLossLimit, false, true, true, false)]
    [InlineData(OrderType.TakeProfitLimit, false, true, true, false)]
    public void Classification_MatchesTheType(
        OrderType type,
        bool isImmediate,
        bool isLeveled,
        bool isLimit,
        bool isMarket
    )
    {
        // arrange
        var request = Request(type, 10m, 9m);

        // assert
        request.IsImmediate().Is(isImmediate);
        request.IsLeveled().Is(isLeveled);
        request.IsLimit().Is(isLimit);
        request.IsMarket().Is(isMarket);
    }

    /// <summary>
    /// Verifies that the target price is the limit price where the type has one, the trigger price where it
    /// waits on one, and zero for a plain market order.
    /// </summary>
    /// <param name="type">The order type under test.</param>
    /// <param name="expected">The price the request is aimed at.</param>
    [Theory]
    [InlineData(OrderType.Limit, 10)]
    [InlineData(OrderType.Market, 0)]
    [InlineData(OrderType.StopLossMarket, 9)]
    [InlineData(OrderType.TakeProfitMarket, 9)]
    [InlineData(OrderType.StopLossLimit, 10)]
    [InlineData(OrderType.TakeProfitLimit, 10)]
    public void TargetPrice_IsThePriceTheTypeAimsAt(OrderType type, int expected)
    {
        // arrange
        var request = Request(type, 10m, 9m);

        // assert
        request.TargetPrice().Is(expected);
    }

    /// <summary>
    /// Verifies that converting a modification into an init request takes the new terms from the modification
    /// and everything identifying the order from the order itself — so the request describes the same order,
    /// on the same instrument, under the same identifier.
    /// </summary>
    [Fact]
    public void ToInitOrderRequest_TakesTermsFromTheChangeAndIdentityFromTheOrder()
    {
        // arrange
        var order = new OrderModel(
            "exchange-id",
            "client-id",
            OrientationRange.Short,
            "BTCUSDT",
            OrderSide.Buy,
            OrderType.Limit,
            1m,
            10m,
            0m,
            true,
            0L,
            OrderStatus.New,
            0m,
            0m,
            0L
        );

        // act - a change of side, type, quantity and both prices
        var request = ModifyToStopLossLimitOrder(order, OrderSide.Sell, 3m, 8m, 9m).ToInitOrderRequest();

        // assert - the new terms come from the modification
        request.Side.Is(OrderSide.Sell);
        request.Type.Is(OrderType.StopLossLimit);
        request.Qty.Is(3m);
        request.Price.Is(8m);
        request.LevelPrice.Is(9m);

        // assert - identity and placement come from the order being modified
        request.Id.Is(order.ClientOrderId, "the modified order is re-placed under its own client id");
        request.Symbol.Is(order.Symbol);
        request.Range.Is(order.Range);
        request.ReduceOnly.Is(order.ReduceOnly);
    }

    /// <summary>
    /// Builds an init request of the given type through the factory for it, so the price each type actually
    /// carries is the one under test rather than one this test chose.
    /// </summary>
    /// <param name="type">The order type to build a request for.</param>
    /// <param name="price">The limit price, for the types that carry one.</param>
    /// <param name="levelPrice">The trigger price, for the types that wait on one.</param>
    /// <returns>The request.</returns>
    private static IInitOrderRequest Request(OrderType type, decimal price, decimal levelPrice) =>
        type switch
        {
            OrderType.Limit => InitLimitOrder("id", OrientationRange.Both, "BTCUSDT", OrderSide.Buy, 1m, price),
            OrderType.Market => InitMarketOrder("id", OrientationRange.Both, "BTCUSDT", OrderSide.Buy, 1m),
            OrderType.StopLossMarket => InitStopLossMarketOrder(
                "id",
                OrientationRange.Both,
                "BTCUSDT",
                OrderSide.Buy,
                1m,
                levelPrice
            ),
            OrderType.TakeProfitMarket => InitTakeProfitMarketOrder(
                "id",
                OrientationRange.Both,
                "BTCUSDT",
                OrderSide.Buy,
                1m,
                levelPrice
            ),
            OrderType.StopLossLimit => InitStopLossLimitOrder(
                "id",
                OrientationRange.Both,
                "BTCUSDT",
                OrderSide.Buy,
                1m,
                price,
                levelPrice
            ),
            _ => InitTakeProfitLimitOrder("id", OrientationRange.Both, "BTCUSDT", OrderSide.Buy, 1m, price, levelPrice),
        };
}
