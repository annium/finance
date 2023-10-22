using System;
using Annium.Finance.Providers.Abstractions.Domain.Enums;
using NodaTime;

namespace Annium.Finance.Providers.Abstractions.Domain.Dto;

public sealed record OrderDto(
    Guid Id,
    string Symbol,
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
);