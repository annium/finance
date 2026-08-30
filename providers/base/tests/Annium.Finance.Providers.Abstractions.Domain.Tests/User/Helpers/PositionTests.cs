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
