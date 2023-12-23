using Annium.Finance.Providers.Abstractions.Domain.Enums;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Contracts.User.Domain;

internal sealed record OrderUpdateEvent(
    string Symbol,
    string TradeId,
    string OrderId,
    string ClientOrderId,
    OrderType Type,
    OrderSide Side,
    decimal TotalQty,
    decimal Price,
    decimal LevelPrice,
    bool ReduceOnly,
    OrderStatus Status,
    decimal ExecutedQty,
    decimal ExecutedPrice,
    decimal LastExecutedQty,
    decimal LastExecutedPrice,
    decimal CommissionAmount,
    string CommissionAsset,
    bool IsMaker,
    long CreatedDate,
    long UpdatedDate
);
