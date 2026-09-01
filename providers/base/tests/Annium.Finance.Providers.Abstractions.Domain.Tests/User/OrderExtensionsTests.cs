using System;
using Annium.Finance.Providers.Abstractions.Domain.User;
using Annium.Finance.Providers.Tests.Lib.User;
using Annium.Finance.Providers.Tests.Lib.User.Operations;
using Annium.Testing;
using Xunit;

namespace Annium.Finance.Providers.Abstractions.Domain.Tests.User;

/// <summary>
/// Pins the status and type classifications <see cref="OrderExtensions"/> derives from an <see cref="IOrder"/>,
/// exercised across the order lifecycle produced by the <c>Tests.Lib</c> position/order fixtures.
/// </summary>
public class OrderExtensionsTests
{
    /// <summary>
    /// Verifies that an order's target price is its limit price where its type carries one, its trigger price
    /// where it waits on one, and zero for a plain market order — the same rule the request side applies
    /// before the order exists. Nothing asserted this: it reaches tests only through
    /// <see cref="Order.ToString"/>, which they match on unrelated substrings.
    /// </summary>
    /// <param name="type">The order type under test.</param>
    /// <param name="expected">The price the order is aimed at.</param>
    [Theory]
    [InlineData(OrderType.Limit, 10)]
    [InlineData(OrderType.Market, 0)]
    [InlineData(OrderType.StopLossMarket, 9)]
    [InlineData(OrderType.TakeProfitMarket, 9)]
    [InlineData(OrderType.StopLossLimit, 10)]
    [InlineData(OrderType.TakeProfitLimit, 10)]
    public void TargetPrice_IsThePriceTheTypeAimsAt(OrderType type, int expected)
    {
        // arrange - a limit price of 10 and a trigger of 9, so the two can never be mistaken for each other
        var position = PositionHelper.CreatePosition(1);
        var isLimitPriced = type is OrderType.Limit or OrderType.StopLossLimit or OrderType.TakeProfitLimit;
        var isLeveled = type is not (OrderType.Limit or OrderType.Market);
        var order = new Annium.Finance.Providers.Tests.Lib.User.Order(
            Guid.NewGuid(),
            position,
            OrderSide.Buy,
            type,
            1m,
            isLimitPriced ? 10m : 0m,
            isLeveled ? 9m : 0m,
            0L,
            OrderStatus.New,
            0m,
            0m,
            0m,
            0L
        );

        // assert
        order.TargetPrice().Is(expected);
    }

    /// <summary>Verifies that <see cref="OrderExtensions.IsActive{TOrder}"/> is true for new and partially filled orders, false once filled or canceled.</summary>
    [Fact]
    public void IsActive()
    {
        // arrange
        var position = PositionHelper.CreatePosition(1);

        // assert
        position.AddLimitBuyOrder(2, 1).Data.IsActive().IsTrue();
        position.AddLimitBuyOrder(2, 1).FillPartially(1).Data.IsActive().IsTrue();
        position.AddLimitBuyOrder(2, 1).Fill().Data.IsActive().IsFalse();
        position.AddLimitBuyOrder(2, 1).Cancel().Data.IsActive().IsFalse();
    }

    /// <summary>Verifies that <see cref="OrderExtensions.IsInactive{TOrder}"/> is the exact inverse of <see cref="OrderExtensions.IsActive{TOrder}"/>.</summary>
    [Fact]
    public void IsInactive()
    {
        // arrange
        var position = PositionHelper.CreatePosition(1);

        // assert
        position.AddLimitBuyOrder(2, 1).Data.IsInactive().IsFalse();
        position.AddLimitBuyOrder(2, 1).FillPartially(1).Data.IsInactive().IsFalse();
        position.AddLimitBuyOrder(2, 1).Fill().Data.IsInactive().IsTrue();
        position.AddLimitBuyOrder(2, 1).Cancel().Data.IsInactive().IsTrue();
    }

    /// <summary>Verifies that <see cref="OrderExtensions.IsImmediate{TOrder}"/> is true for a limit order but false for stop-loss and take-profit orders.</summary>
    [Fact]
    public void IsImmediate()
    {
        // arrange
        var position = PositionHelper.CreatePosition(1);

        // assert
        position.AddLimitBuyOrder(2, 1).Fill().Data.IsImmediate().IsTrue();
        position.AddStopLossMarketSellOrder(1, 0.5m).Data.IsImmediate().IsFalse();
        position.AddTakeProfitMarketSellOrder(1, 1.5m).Data.IsImmediate().IsFalse();
    }

    /// <summary>
    /// An order is leveled when it waits for a trigger price: stop-loss and take-profit orders are, a plain
    /// limit order is not. This is not the same question as <see cref="OrderExtensions.IsLimit{TOrder}"/>
    /// asks - the stop-loss and take-profit limit types answer yes to both - which is why this test used to
    /// assert nothing about its own subject: it called IsLimit on all three cases instead.
    /// </summary>
    [Fact]
    public void IsLeveled()
    {
        // arrange
        var position = PositionHelper.CreatePosition(1);

        // assert
        position.AddLimitBuyOrder(2, 1).Fill().Data.IsLeveled().IsFalse();
        position.AddStopLossMarketSellOrder(1, 0.5m).Data.IsLeveled().IsTrue();
        position.AddTakeProfitMarketSellOrder(1, 1.5m).Data.IsLeveled().IsTrue();
    }

    /// <summary>Verifies that <see cref="OrderExtensions.IsLimit{TOrder}"/> is true only for a limit order, false for stop-loss and take-profit market orders.</summary>
    [Fact]
    public void IsLimit()
    {
        // arrange
        var position = PositionHelper.CreatePosition(1);

        // assert
        position.AddLimitBuyOrder(2, 1).Fill().Data.IsLimit().IsTrue();
        position.AddStopLossMarketSellOrder(1, 0.5m).Data.IsLimit().IsFalse();
        position.AddTakeProfitMarketSellOrder(1, 1.5m).Data.IsLimit().IsFalse();
    }

    /// <summary>Verifies that <see cref="OrderExtensions.IsMarket{TOrder}"/> is false for a limit order but true for stop-loss and take-profit market orders.</summary>
    [Fact]
    public void IsMarket()
    {
        // arrange
        var position = PositionHelper.CreatePosition(1);

        // assert
        position.AddLimitBuyOrder(2, 1).Fill().Data.IsMarket().IsFalse();
        position.AddStopLossMarketSellOrder(1, 0.5m).Data.IsMarket().IsTrue();
        position.AddTakeProfitMarketSellOrder(1, 1.5m).Data.IsMarket().IsTrue();
    }

    /// <summary>
    /// Verifies that <see cref="OrderExtensions.OpeningQty{TOrder}"/> tracks the unfilled quantity while the order
    /// is active, and drops to zero once the order fills or is canceled - regardless of how much was filled
    /// before the cancel.
    /// </summary>
    [Fact]
    public void OpeningQty()
    {
        // arrange
        var position = PositionHelper.CreatePosition(1);

        // assert
        position.AddLimitBuyOrder(2, 1).Data.OpeningQty().Is(2);
        position.AddLimitBuyOrder(2, 1).FillPartially(1).Data.OpeningQty().Is(1);
        position.AddLimitBuyOrder(2, 1).Fill().Data.OpeningQty().Is(0);
        position.AddLimitBuyOrder(2, 1).FillPartially(0.5m).Cancel().Data.OpeningQty().Is(0);
    }
}
