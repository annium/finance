using Annium.Finance.Providers.Abstractions.Domain.User;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Internal.User.Contracts.Domain;

internal sealed record OrderUpdateEvent(
    string Symbol,
    string TradeId,
    string OrderId,
    string ClientOrderId,
    OrientationRange Range,
    OrderType Type,
    OrderSide Side,
    decimal TotalQty,
    decimal Price,
    decimal LevelPrice,
    OrderStatus Status,
    decimal ExecutedQty,
    decimal ExecutedPrice,
    decimal LastExecutedQty,
    decimal LastExecutedPrice,
    decimal CommissionAmount,
    string CommissionAsset,
    bool IsMaker,
    long CreatedAt,
    long UpdatedAt
);
