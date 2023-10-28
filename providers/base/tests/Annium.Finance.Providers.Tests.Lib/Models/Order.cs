using System;
using Annium.Finance.Providers.Abstractions.Domain.Enums;
using Annium.Finance.Providers.Abstractions.Domain.Extensions;
using Annium.Finance.Providers.Abstractions.Domain.Interfaces;
using NodaTime;

namespace Annium.Finance.Providers.Tests.Lib.Models;

public sealed record Order(
    Guid Id,
    Position Position,
    OrderSide Side,
    OrderType Type,
    decimal TotalQty,
    decimal Price,
    decimal LevelPrice,
    Instant CreatedAt,
    OrderStatus Status,
    decimal ExecutedQty,
    decimal ExecutedPrice,
    decimal Fee,
    Instant UpdatedAt
) : IOrder<Position, Instrument, Resource>
{
    public Guid PositionId { get; } = Position.Id;

    public OrderStatus Status { get; private set; } = Status;
    public decimal ExecutedQty { get; private set; } = ExecutedQty;
    public decimal ExecutedPrice { get; private set; } = ExecutedPrice;
    public decimal Fee { get; private set; } = Fee;
    public Instant UpdatedAt { get; private set; } = UpdatedAt;

    public Order Update(OrderStatus status, decimal executedQty, decimal executedPrice, decimal fee, Instant now)
    {
        this.ValidateStatus(OrderStatus.New, OrderStatus.PartiallyFilled, OrderStatus.Canceled);

        var prevExecutedQty = ExecutedQty;
        var prevFee = Fee;

        Status = status;
        ExecutedQty = executedQty;
        ExecutedPrice = executedPrice;
        Fee = fee;
        UpdatedAt = now;

        this.ValidateQtyAndPrice();

        Position.UpdateOrder(
            Id,
            Side,
            ExecutedQty,
            ExecutedPrice,
            this.CancellableQty(),
            Fee,
            prevExecutedQty,
            prevFee,
            UpdatedAt
        );

        return this;
    }

    public override string ToString() =>
        $"{Status} {Type} {Side} {ExecutedQty}/{TotalQty} for {Price}({LevelPrice}) at {this.TargetPrice()}({ExecutedPrice}) [id:{Id},pid:{PositionId}], {CreatedAt:dd.MM.yyyy HH:mm:ss} / {UpdatedAt:dd.MM.yyyy HH:mm:ss}";
}
