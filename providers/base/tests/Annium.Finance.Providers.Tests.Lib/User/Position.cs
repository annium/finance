using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Annium.Data.Operations;
using Annium.Finance.Providers.Abstractions.Domain.User;
using Annium.Finance.Providers.Tests.Lib.Market;

namespace Annium.Finance.Providers.Tests.Lib.User;

/// <summary>
/// A fake leveraged position that tracks its orders and derives its own opened/closed/borrowed quantities and
/// sums as they fill, mirroring the bookkeeping a real exchange does, so tests can compare it against what a
/// provider actually reports.
/// </summary>
/// <param name="Id">The position's unique identifier.</param>
/// <param name="Instrument">The instrument the position is held on.</param>
/// <param name="CreatedAt">The moment the position was created, in Unix milliseconds.</param>
/// <param name="OrientationRange">Whether the position can be long, short, or either.</param>
/// <param name="MarginType">The initial margin type (cross or isolated).</param>
/// <param name="Leverage">The initial leverage multiplier applied to the position.</param>
/// <param name="UpdatedAt">The moment the position was last updated, in Unix milliseconds.</param>
/// <param name="TotalQty">The initial total quantity across opening and closing orders.</param>
/// <param name="Price">The initial average opening price.</param>
/// <param name="OpeningQty">The initial quantity still pending in opening orders.</param>
/// <param name="OpenedQty">The initial quantity already opened.</param>
/// <param name="OpenedSum">The initial notional sum of the opened quantity.</param>
/// <param name="OpenedFee">The initial fee charged on opening orders.</param>
/// <param name="ClosingQty">The initial quantity still pending in closing orders.</param>
/// <param name="ClosedQty">The initial quantity already closed.</param>
/// <param name="ClosedSum">The initial notional sum of the closed quantity.</param>
/// <param name="ClosedFee">The initial fee charged on closing orders.</param>
/// <param name="BorrowedQty">The initial quantity borrowed to support the leveraged position.</param>
/// <param name="BorrowedSum">The initial notional sum of the borrowed quantity.</param>
public sealed record Position(
    Guid Id,
    Instrument Instrument,
    long CreatedAt,
    OrientationRange OrientationRange,
    MarginType MarginType,
    decimal Leverage,
    long UpdatedAt,
    decimal TotalQty,
    decimal Price,
    decimal OpeningQty,
    decimal OpenedQty,
    decimal OpenedSum,
    decimal OpenedFee,
    decimal ClosingQty,
    decimal ClosedQty,
    decimal ClosedSum,
    decimal ClosedFee,
    decimal BorrowedQty,
    decimal BorrowedSum
) : IPosition
{
    /// <summary>Gets the current margin type (cross or isolated).</summary>
    public MarginType MarginType { get; private set; } = MarginType;

    /// <summary>Gets the current leverage multiplier applied to the position.</summary>
    public decimal Leverage { get; private set; } = Leverage;

    /// <summary>Gets a value indicating whether the position currently has an open orientation.</summary>
    public bool IsActive => OrientationType is not null;

    /// <summary>Gets the position's current orientation (long/short), or null while it has none.</summary>
    public OrientationType? OrientationType { get; private set; }

    /// <summary>Gets the moment the position was last updated, in Unix milliseconds.</summary>
    public long UpdatedAt { get; private set; } = UpdatedAt;

    /// <summary>Gets the current total quantity across opening and closing orders.</summary>
    public decimal TotalQty { get; private set; } = TotalQty;

    /// <summary>Gets the current average opening price.</summary>
    public decimal Price { get; private set; } = Price;

    /// <summary>Gets the current quantity still pending in opening orders.</summary>
    public decimal OpeningQty { get; private set; } = OpeningQty;

    /// <summary>Gets the current quantity already opened.</summary>
    public decimal OpenedQty { get; private set; } = OpenedQty;

    /// <summary>Gets the current notional sum of the opened quantity.</summary>
    public decimal OpenedSum { get; private set; } = OpenedSum;

    /// <summary>Gets the current fee charged on opening orders.</summary>
    public decimal OpenedFee { get; private set; } = OpenedFee;

    /// <summary>Gets the current quantity still pending in closing orders.</summary>
    public decimal ClosingQty { get; private set; } = ClosingQty;

    /// <summary>Gets the current quantity already closed.</summary>
    public decimal ClosedQty { get; private set; } = ClosedQty;

    /// <summary>Gets the current notional sum of the closed quantity.</summary>
    public decimal ClosedSum { get; private set; } = ClosedSum;

    /// <summary>Gets the current fee charged on closing orders.</summary>
    public decimal ClosedFee { get; private set; } = ClosedFee;

    /// <summary>Gets the current quantity borrowed to support the leveraged position.</summary>
    public decimal BorrowedQty { get; private set; } = BorrowedQty;

    /// <summary>Gets the current notional sum of the borrowed quantity.</summary>
    public decimal BorrowedSum { get; private set; } = BorrowedSum;

    /// <summary>Gets the identifiers of the orders currently tracked against this position.</summary>
    public IReadOnlyCollection<Guid> Orders => _orders;

    /// <summary>
    /// The fraction of the position's opened-minus-closed quantity that is borrowed, given its leverage.
    /// Recomputed whenever the leverage changes - held as a field rather than derived on each read so that
    /// an impossible leverage is rejected where it is set, not later from inside a bookkeeping update.
    /// </summary>
    private decimal _borrowedPart = 1m - 1m / Leverage;

    /// <summary>The identifiers of the orders currently tracked against this position.</summary>
    private readonly List<Guid> _orders = new();

    /// <summary>
    /// Registers a newly placed order with the position and rolls its quantity into opening or closing totals
    /// depending on whether it opens or closes the position's current orientation.
    /// </summary>
    /// <param name="orderId">The identifier of the order being added.</param>
    /// <param name="side">The side (buy or sell) the order was placed on.</param>
    /// <param name="totalQty">The total quantity the order was placed for.</param>
    /// <param name="createdAt">The moment the order was created, in Unix milliseconds.</param>
    /// <param name="result">The result to report an error to if the order is already tracked.</param>
    public void AddOrder(Guid orderId, OrderSide side, decimal totalQty, long createdAt, IResult<Order> result)
    {
        if (_orders.Contains(orderId))
        {
            result.Error($"Position {this} already tracks order {orderId}");
            return;
        }

        _orders.Add(orderId);

        if (IsOpenOrder(side))
        {
            TotalQty += totalQty;
            OpeningQty += totalQty;
        }
        else
        {
            ClosingQty += totalQty;
        }

        SyncState(createdAt);
        // AssertValidity();
    }

    /// <summary>
    /// Rolls an order's incremental fill into the position: resolves the position's orientation if it wasn't
    /// set yet, updates the running average price for opening fills, and advances the opened/closed quantity,
    /// sum and fee totals.
    /// </summary>
    /// <param name="orderId">The identifier of the order being updated.</param>
    /// <param name="side">The side (buy or sell) the order was placed on.</param>
    /// <param name="executedQty">The order's new total quantity filled.</param>
    /// <param name="executedPrice">The order's new volume-weighted average fill price.</param>
    /// <param name="cancellableQty">The quantity no longer pending because the order was canceled.</param>
    /// <param name="fee">The order's new total fee.</param>
    /// <param name="prevExecutedQty">The order's quantity filled before this update.</param>
    /// <param name="prevFee">The order's fee before this update.</param>
    /// <param name="updatedAt">The moment of the update, in Unix milliseconds.</param>
    /// <param name="result">The result to report an error to if the order is not tracked.</param>
    public void UpdateOrder(
        Guid orderId,
        OrderSide side,
        decimal executedQty,
        decimal executedPrice,
        decimal cancellableQty,
        decimal fee,
        decimal prevExecutedQty,
        decimal prevFee,
        long updatedAt,
        IResult<Order> result
    )
    {
        if (!_orders.Contains(orderId))
        {
            result.Error($"Position {this} has not been tracking order {orderId}");
            return;
        }

        TrySetOrientation(side, executedQty);

        var newQty = executedQty - prevExecutedQty;
        var newSum = newQty * executedPrice;

        if (IsOpenOrder(side))
        {
            Price = PositionHelper.ResolvePrice(OpenedQty - ClosedQty, Price, newQty, executedPrice);

            OpeningQty -= newQty + cancellableQty;
            OpenedQty += newQty;
            OpenedSum += newSum;
            OpenedFee += fee - prevFee;
        }
        else
        {
            ClosingQty -= newQty + cancellableQty;
            ClosedQty += newQty;
            ClosedSum += newSum;
            ClosedFee += fee - prevFee;
        }

        SyncState(updatedAt);
        TryResetOrientation();
        // AssertValidity();
    }

    /// <summary>
    /// Stops tracking a canceled order and reverses whatever quantity/sum/fee it had already contributed to
    /// the position's opened or closed totals.
    /// </summary>
    /// <param name="orderId">The identifier of the order being removed.</param>
    /// <param name="side">The side (buy or sell) the order was placed on.</param>
    /// <param name="totalQty">The order's total quantity.</param>
    /// <param name="potentialQty">The quantity the order could still have filled before cancellation.</param>
    /// <param name="executedQty">The quantity the order had already filled.</param>
    /// <param name="executedSum">The notional value the order actually booked, accumulated fill by fill.</param>
    /// <param name="fee">The fee already charged on the order.</param>
    /// <param name="updatedAt">The moment of the removal, in Unix milliseconds.</param>
    /// <param name="result">The result to report an error to if the order is not tracked.</param>
    public void RemoveOrder(
        Guid orderId,
        OrderSide side,
        decimal totalQty,
        decimal potentialQty,
        decimal executedQty,
        decimal executedSum,
        decimal fee,
        long updatedAt,
        IResult<Order> result
    )
    {
        if (!_orders.Remove(orderId))
        {
            result.Error($"Position {this} has not been tracking order {orderId}");
            return;
        }

        // reverse the sum that was booked, not one recomputed from the final price. UpdateOrder adds each
        // fill at the price that fill happened at, so for an order filled 5@100 then 5@110 it booked
        // 1050 - while 10 times the last price is 1100, and removing that left the position 50 richer
        // than it ever was
        if (IsOpenOrder(side))
        {
            TotalQty -= totalQty;
            OpeningQty -= potentialQty - executedQty;
            OpenedQty -= executedQty;
            OpenedSum -= executedSum;
            OpenedFee -= fee;
        }
        else
        {
            ClosingQty -= potentialQty - executedQty;
            ClosedQty -= executedQty;
            ClosedSum -= executedSum;
            ClosedFee -= fee;
        }

        SyncState(updatedAt);
        TryResetOrientation();
        // AssertValidity();
    }

    /// <summary>
    /// Updates the position's margin type and leverage, and rebalances how much of it is borrowed at the
    /// new leverage.
    /// </summary>
    /// <param name="marginType">The new margin type (cross or isolated).</param>
    /// <param name="leverage">The new leverage multiplier.</param>
    /// <returns>This position, for chaining.</returns>
    public Position Update(MarginType marginType, decimal leverage)
    {
        MarginType = marginType;
        Leverage = leverage;

        // the borrowed part is a function of the leverage, so it has to move with it. Left at the value
        // it was constructed with, every later update went on borrowing at a leverage the position no
        // longer had
        _borrowedPart = 1m - 1m / Leverage;
        SyncState(UpdatedAt);

        return this;
    }

    /// <summary>Returns a human-readable summary of the position's orientation and tracked orders, for trace logging.</summary>
    /// <returns>A human-readable summary of the position.</returns>
    public override string ToString() =>
        $"({OrientationType?.ToString() ?? "inactive"}) {Instrument} with {_orders.Count} order(s) [id:{Id}]";

    /// <summary>
    /// Recomputes the borrowed quantity/sum from the current opened/closed quantities and price, and advances
    /// the position's last-updated timestamp.
    /// </summary>
    /// <param name="updatedAt">The moment of the update, in Unix milliseconds.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SyncState(long updatedAt)
    {
        BorrowedQty = (OpenedQty - ClosedQty) * _borrowedPart;
        BorrowedSum = BorrowedQty * Price;

        UpdatedAt = Math.Max(UpdatedAt, updatedAt);
    }

    /// <summary>
    /// Sets the position's orientation from the first order that actually fills, if it isn't set yet.
    /// </summary>
    /// <param name="side">The side (buy or sell) of the order that filled.</param>
    /// <param name="executedQty">The quantity the order filled.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void TrySetOrientation(OrderSide side, decimal executedQty)
    {
        // this method can't change orientation - only set, when it's unset
        if (OrientationType is not null)
            return;

        // if order is not executed - orientation is assumed to be not defined
        if (executedQty == 0)
            return;

        // as far as orientation type is null - order is open, thus orientation type can be resolved
        OrientationType =
            side is OrderSide.Buy
                ? Abstractions.Domain.User.OrientationType.Long
                : Abstractions.Domain.User.OrientationType.Short;
    }

    /// <summary>
    /// Clears the position's orientation once its opened and closed quantities are equal, i.e. it is flat again.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void TryResetOrientation()
    {
        // this method obviously does nothing, when orientation is not set
        if (OrientationType is null)
            return;

        if (OpenedQty == ClosedQty)
            OrientationType = null;
    }

    /// <summary>
    /// Determines whether an order on the given side opens the position (as opposed to closing it), based on
    /// the position's current orientation. While the position has no orientation yet, every order opens it.
    /// </summary>
    /// <param name="side">The side (buy or sell) the order was placed on.</param>
    /// <returns>True if the order opens the position; false if it closes it.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsOpenOrder(OrderSide side)
    {
        if (OrientationType is null)
            return true;

        return OrientationType is Abstractions.Domain.User.OrientationType.Long
            ? side is OrderSide.Buy
            : side is OrderSide.Sell;
    }
}
