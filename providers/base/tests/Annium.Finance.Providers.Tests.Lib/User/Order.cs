using System;
using Annium.Data.Operations;
using Annium.Finance.Providers.Abstractions.Domain.User;
using Annium.Finance.Providers.Tests.Lib.Shared.Operations;

namespace Annium.Finance.Providers.Tests.Lib.User;

/// <summary>
/// A fake order tracked against a fake <see cref="Position"/>, used to model the local, expected state of an
/// order placed with a provider so it can be compared to what the provider actually reports.
/// </summary>
/// <param name="Id">The order's unique identifier.</param>
/// <param name="Position">The position the order was placed against.</param>
/// <param name="Side">The side (buy or sell) the order was placed on.</param>
/// <param name="Type">The type of the order.</param>
/// <param name="TotalQty">The total quantity the order was placed for, in the instrument's base asset.</param>
/// <param name="Price">The limit price of the order; zero for market and stop/take-profit market orders.</param>
/// <param name="LevelPrice">The trigger price of a stop/take-profit order; zero for orders that are not leveled.</param>
/// <param name="CreatedAt">The moment the order was created, in Unix milliseconds.</param>
/// <param name="Status">The initial lifecycle status of the order.</param>
/// <param name="ExecutedQty">The initial quantity filled, in the instrument's base asset.</param>
/// <param name="ExecutedPrice">The initial volume-weighted average fill price.</param>
/// <param name="Fee">The initial fee charged on the order.</param>
/// <param name="UpdatedAt">The moment the order was last updated, in Unix milliseconds.</param>
public sealed record Order(
    Guid Id,
    Position Position,
    OrderSide Side,
    OrderType Type,
    decimal TotalQty,
    decimal Price,
    decimal LevelPrice,
    long CreatedAt,
    OrderStatus Status,
    decimal ExecutedQty,
    decimal ExecutedPrice,
    decimal Fee,
    long UpdatedAt
) : IOrder
{
    /// <summary>Gets the identifier of the position this order was placed against.</summary>
    public Guid PositionId { get; } = Position.Id;

    /// <summary>Gets the current lifecycle status of the order.</summary>
    public OrderStatus Status { get; private set; } = Status;

    /// <summary>Gets the quantity filled so far, in the instrument's base asset.</summary>
    public decimal ExecutedQty { get; private set; } = ExecutedQty;

    /// <summary>Gets the volume-weighted average price the order has been filled at so far.</summary>
    public decimal ExecutedPrice { get; private set; } = ExecutedPrice;

    /// <summary>Gets the fee charged on the order so far.</summary>
    public decimal Fee { get; private set; } = Fee;

    /// <summary>Gets the moment the order was last updated, in Unix milliseconds.</summary>
    public long UpdatedAt { get; private set; } = UpdatedAt;

    /// <summary>
    /// Advances the order to a new status/fill state and reports the change to its position.
    /// </summary>
    /// <param name="status">The new lifecycle status of the order.</param>
    /// <param name="executedQty">The new total quantity filled.</param>
    /// <param name="executedPrice">The new volume-weighted average fill price.</param>
    /// <param name="fee">The new total fee charged on the order.</param>
    /// <param name="now">The moment of the update, in Unix milliseconds.</param>
    /// <returns>A result carrying the order on success, or the validation errors that blocked the update.</returns>
    public IResult<Order> Update(OrderStatus status, decimal executedQty, decimal executedPrice, decimal fee, long now)
    {
        var result = this.AsResult()
            .ValidateStatus(OrderStatus.New, OrderStatus.PartiallyFilled, OrderStatus.Canceled)
            .ValidateCanProcess(status, executedQty, executedPrice);

        if (result.HasErrors)
            return result;

        Position.UpdateOrder(
            Id,
            Side,
            executedQty,
            executedPrice,
            status is OrderStatus.Canceled ? TotalQty - executedQty : 0,
            // the new fee and the one being replaced, in that order. Passing the old value for both made
            // the position's `fee - prevFee` identically zero, so its opened/closed fee totals stayed at
            // zero however much an order was charged, and anything comparing them to a real one matched
            fee,
            ExecutedQty,
            Fee,
            UpdatedAt,
            result
        );

        if (result.HasErrors)
            return result;

        Status = status;
        ExecutedQty = executedQty;
        ExecutedPrice = executedPrice;
        Fee = fee;
        UpdatedAt = now;

        return result;
    }

    /// <summary>Returns a human-readable summary of the order's terms and fill state, for trace logging.</summary>
    /// <returns>A human-readable summary of the order.</returns>
    public override string ToString() =>
        $"{Status} {Type} {Side} {ExecutedQty}/{TotalQty} for {Price}({LevelPrice}) at {this.TargetPrice()}({ExecutedPrice}) [id:{Id},pid:{PositionId}], {CreatedAt:dd.MM.yyyy HH:mm:ss} / {UpdatedAt:dd.MM.yyyy HH:mm:ss}";
}
