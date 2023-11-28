using Annium.Finance.Providers.Abstractions.Domain.Enums;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Contracts.User.Domain;

internal sealed record OrderUpdateEvent(
    string Symbol,
    string TradeId,
    string OrderId,
    string ClientOrderId,
    OrderType Type,
    OrderSide Side,
    decimal Quantity,
    decimal Price,
    decimal TriggerPrice,
    bool ReduceOnly,
    OrderStatus Status,
    decimal ExecutedQuantity,
    decimal ExecutedPrice,
    decimal LastExecutedQuantity,
    decimal LastExecutedPrice,
    decimal CommissionAmount,
    string CommissionAsset,
    bool IsMaker,
    long CreatedDate,
    long UpdatedDate
);
