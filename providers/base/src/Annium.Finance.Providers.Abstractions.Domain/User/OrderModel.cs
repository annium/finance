namespace Annium.Finance.Providers.Abstractions.Domain.User;

public sealed record OrderModel(
    string Id,
    string ClientOrderId,
    OrientationRange Range,
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
