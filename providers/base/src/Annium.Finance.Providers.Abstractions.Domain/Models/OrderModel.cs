using Annium.Finance.Providers.Abstractions.Domain.Enums;
using Annium.Finance.Providers.Abstractions.Domain.Interfaces;

namespace Annium.Finance.Providers.Abstractions.Domain.Models;

public sealed record OrderModel(
    string Id,
    string ClientOrderId,
    string Symbol,
    OrderSide Side,
    OrderType Type,
    decimal TotalQty,
    decimal Price,
    decimal LevelPrice,
    bool ReduceOnly,
    long CreatedAt,
    OrderStatus Status,
    decimal ExecutedQty,
    decimal ExecutedPrice,
    long UpdatedAt
) : IOrder;
