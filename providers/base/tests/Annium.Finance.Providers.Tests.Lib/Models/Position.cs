using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Annium.Data.Operations;
using Annium.Finance.Providers.Abstractions.Domain.User;
using Annium.Finance.Providers.Tests.Lib.Models.Helpers;

namespace Annium.Finance.Providers.Tests.Lib.Models;

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
    public MarginType MarginType { get; private set; } = MarginType;
    public decimal Leverage { get; private set; } = Leverage;
    public bool IsActive => OrientationType is not null;
    public OrientationType? OrientationType { get; private set; }
    public long UpdatedAt { get; private set; } = UpdatedAt;
    public decimal TotalQty { get; private set; } = TotalQty;
    public decimal Price { get; private set; } = Price;
    public decimal OpeningQty { get; private set; } = OpeningQty;
    public decimal OpenedQty { get; private set; } = OpenedQty;
    public decimal OpenedSum { get; private set; } = OpenedSum;
    public decimal OpenedFee { get; private set; } = OpenedFee;
    public decimal ClosingQty { get; private set; } = ClosingQty;
    public decimal ClosedQty { get; private set; } = ClosedQty;
    public decimal ClosedSum { get; private set; } = ClosedSum;
    public decimal ClosedFee { get; private set; } = ClosedFee;
    public decimal BorrowedQty { get; private set; } = BorrowedQty;
    public decimal BorrowedSum { get; private set; } = BorrowedSum;
    public IReadOnlyCollection<Guid> Orders => _orders;
    private readonly decimal _borrowedPart = 1m - 1m / Leverage;
    private readonly List<Guid> _orders = new();

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

    public void RemoveOrder(
        Guid orderId,
        OrderSide side,
        decimal totalQty,
        decimal potentialQty,
        decimal executedQty,
        decimal executedPrice,
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

        if (IsOpenOrder(side))
        {
            TotalQty -= totalQty;
            OpeningQty -= potentialQty - executedQty;
            OpenedQty -= executedQty;
            OpenedSum -= executedQty * executedPrice;
            OpenedFee -= fee;
        }
        else
        {
            ClosingQty -= potentialQty - executedQty;
            ClosedQty -= executedQty;
            ClosedSum -= executedQty * executedPrice;
            ClosedFee -= fee;
        }

        SyncState(updatedAt);
        TryResetOrientation();
        // AssertValidity();
    }

    public Position Update(MarginType marginType, decimal leverage)
    {
        MarginType = marginType;
        Leverage = leverage;

        return this;
    }

    public override string ToString() =>
        $"({OrientationType?.ToString() ?? "inactive"}) {Instrument} with {_orders.Count} order(s) [id:{Id}]";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SyncState(long updatedAt)
    {
        BorrowedQty = (OpenedQty - ClosedQty) * _borrowedPart;
        BorrowedSum = BorrowedQty * Price;

        UpdatedAt = Math.Max(UpdatedAt, updatedAt);
    }

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void TryResetOrientation()
    {
        // this method obviously does nothing, when orientation is not set
        if (OrientationType is null)
            return;

        if (OpenedQty == ClosedQty)
            OrientationType = null;
    }

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
