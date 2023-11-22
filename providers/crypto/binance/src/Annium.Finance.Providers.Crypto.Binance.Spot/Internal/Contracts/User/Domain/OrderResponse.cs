using System;
using Annium.Finance.Providers.Abstractions.Domain.Enums;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Internal.Contracts.User.Domain;

internal sealed record OrderResponse(
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
    long UpdatedAt
);
