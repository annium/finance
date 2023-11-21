using System;
using Annium.Finance.Providers.Abstractions.Domain.Enums;

namespace Annium.Finance.Providers.Abstractions.Domain.Dto;

public sealed record OrderDto(
    Guid Id,
    string OrderId,
    string Symbol,
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
);
