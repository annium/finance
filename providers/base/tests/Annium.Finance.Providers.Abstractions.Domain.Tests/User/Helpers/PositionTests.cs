using System;
using Annium.Data.Operations.Testing;
using Annium.Finance.Providers.Abstractions.Domain.User;
using Annium.Finance.Providers.Tests.Lib.User;
using Annium.Finance.Providers.Tests.Lib.User.Operations;
using Annium.Testing;
using Xunit;

namespace Annium.Finance.Providers.Abstractions.Domain.Tests.User.Helpers;

/// <summary>
/// Pins the bookkeeping the fake <see cref="Position"/> and <see cref="Order"/> do as orders are registered
/// and filled. Every exchange-facing test compares what a provider reports against the state these two derive,
/// so a total they get wrong is a comparison that quietly holds.
/// </summary>
public class PositionTests
{
    /// <summary>
    /// A fill's fee reaches the position it was charged against. The position only ever sees the difference
    /// between an order's new fee and its previous one, so passing the same value for both left every
    /// opened/closed fee total sitting at zero however much an order was charged.
    /// </summary>
    [Fact]
    public void Fill_ChargesItsFeeToThePosition()
    {
        // arrange
        var position = PositionHelper.CreatePosition(1);

        // act - two units at a price of ten, so the fee is 2 * 10 * 0.00015
        var result = position.AddLimitBuyOrder(2m, 10m).Fill();

        // assert
        result.HasNoErrors();
        result.Data.Fee.Is(0.003m);
        position.OpenedFee.Is(0.003m, "the fee an order was charged must reach its position");
    }

    /// <summary>
    /// Successive fills accumulate their fees rather than replacing them, which is what the new-minus-previous
    /// difference exists to achieve.
    /// </summary>
    [Fact]
    public void SuccessiveFills_AccumulateTheirFees()
    {
        // arrange
        var position = PositionHelper.CreatePosition(1);

        // act
        var result = position.AddLimitBuyOrder(4m, 10m).FillPartially(1m);
        var afterFirst = position.OpenedFee;
        result.FillPartially(1m);

        // assert
        afterFirst.Is(0.0015m);
        position.OpenedFee.Is(0.003m, "the second fill adds only what it was charged on top of the first");
    }

    /// <summary>
    /// Changing a position's leverage changes how much of it is borrowed. Computed once at construction, the
    /// borrowed fraction went on describing a leverage the position no longer had.
    /// </summary>
    [Fact]
    public void ChangedLeverage_RebalancesTheBorrowedPart()
    {
        // arrange - at 2x, half of what is held is borrowed
        var position = PositionHelper.CreatePosition(2m);
        position.AddLimitBuyOrder(2m, 10m).Fill().HasNoErrors();
        position.BorrowedQty.Is(1m);

        // act - at 4x, three quarters of it is
        position.Update(MarginType.Isolated, 4m);

        // assert
        position.BorrowedQty.Is(1.5m, "the borrowed part must follow the leverage it is derived from");
        position.BorrowedSum.Is(15m);
    }

    /// <summary>
    /// Dropping an order reverses exactly what it booked, leaving the position where it started. Fills at
    /// different prices are the case that matters: the position accumulates each at the price it happened
    /// at, so reversing the total quantity at the last price alone takes out money that was never put in.
    /// </summary>
    [Fact]
    public void RemovedOrder_ReversesWhatItActuallyBooked()
    {
        // arrange - a market order for three, filled twice at different prices: 1@100, then 1@120
        var position = PositionHelper.CreatePosition(1);
        var result = position.AddMarketBuyOrder(3m).FillPartially(1m, 100m);
        result.HasNoErrors();
        result.FillPartially(1m, 120m).HasNoErrors();

        position.OpenedQty.Is(2m);
        position.OpenedSum.Is(220m, "each fill is booked at the price it happened at");
        // and the running average is the two blended, not whichever filled last. This scenario exists to
        // fill at two prices, so it is the one place the averaging is actually exercised
        position.Price.Is(110m, "the position's price is the average of its fills, weighted by quantity");
        // 1*100*0.00015 + 1*120*0.00015, not 2*120*0.00015 - the second fill does not re-price the first
        position.OpenedFee.Is(0.033m, "each fill is charged at the price it happened at");

        // act
        position.RemoveOrder(result.Data).HasNoErrors();

        // assert - the position is back where it started, not 20 richer
        position.Orders.IsEmpty();
        position.OpenedQty.Is(0m);
        position.OpenedSum.Is(0m, "removing an order must reverse exactly the sum it booked");
        position.OpenedFee.Is(0m);
    }

    /// <summary>
    /// A position that has been closed back to flat forgets which way it was pointing, so the next order to
    /// fill decides its direction afresh. Without that, the first fill a position ever sees fixes its
    /// orientation for good — and every later order is then classified as opening or closing against a
    /// direction the position no longer holds, which is the one input all its other bookkeeping turns on.
    /// </summary>
    [Fact]
    public void ClosedPosition_ForgetsItsOrientation()
    {
        // arrange - open long and fill it
        var position = PositionHelper.CreatePosition(1);
        position.AddLimitBuyOrder(2m, 10m).Fill().HasNoErrors();
        position.OrientationType.Is(OrientationType.Long);
        position.IsActive.IsTrue();

        // act - sell the same quantity back, closing the position
        position.AddLimitSellOrder(2m, 10m).Fill().HasNoErrors();

        // assert - flat again, and pointing nowhere
        position.ClosedQty.Is(2m);
        position.OrientationType.Is(null, "a position closed back to flat holds no direction");
        position.IsActive.IsFalse();

        // assert - so the next fill is free to open the other way
        position.AddLimitSellOrder(1m, 10m).Fill().HasNoErrors();
        position.OrientationType.Is(OrientationType.Short, "the next fill opens the position afresh");
    }

    /// <summary>
    /// A position's last-updated moment only ever moves forward. Updates do not arrive in the order they
    /// happened — a fill reported late carries an earlier timestamp than one already applied — and letting
    /// it overwrite the newer moment would make the position claim to be older than what it already holds.
    /// </summary>
    [Fact]
    public void OutOfOrderUpdate_DoesNotMoveTheMomentBackwards()
    {
        // arrange - a fill stamped late
        var position = PositionHelper.CreatePosition(1);
        var result = position.AddLimitBuyOrder(4m, 10m);
        result.HasNoErrors();
        result.Data.Update(OrderStatus.PartiallyFilled, 1m, 10m, 0m, 500L).HasNoErrors();
        position.UpdatedAt.Is(500L);

        // act - and one stamped earlier arriving after it
        result.Data.Update(OrderStatus.PartiallyFilled, 2m, 10m, 0m, 200L).HasNoErrors();

        // assert - the quantity is taken, the clock is not wound back
        position.OpenedQty.Is(2m);
        position.UpdatedAt.Is(500L, "a later update already seen must not be undone by an earlier one");
    }

    /// <summary>
    /// An order that fails validation is not booked against its position. Registering it anyway rolled its
    /// quantity into the position's totals, leaving every later comparison measured against an order the
    /// exchange would have refused.
    /// </summary>
    [Fact]
    public void InvalidOrder_IsNotBookedAgainstThePosition()
    {
        // arrange - an order that is already filled is not a new one, so registering it is invalid
        var position = PositionHelper.CreatePosition(1);
        var order = new Order(
            Guid.NewGuid(),
            position,
            OrderSide.Buy,
            OrderType.Limit,
            2m,
            10m,
            0m,
            0L,
            OrderStatus.Filled,
            2m,
            10m,
            0m,
            0L
        );

        // act
        var result = order.AddToPosition();

        // assert
        result.HasErrors();
        position.Orders.IsEmpty("a rejected order must not be tracked");
        position.TotalQty.Is(0m, "a rejected order must not move the position's totals");
        position.OpeningQty.Is(0m);
    }
}
