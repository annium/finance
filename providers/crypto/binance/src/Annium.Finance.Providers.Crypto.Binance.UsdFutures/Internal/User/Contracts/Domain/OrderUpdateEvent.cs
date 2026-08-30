using Annium.Finance.Providers.Abstractions.Domain.User;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.User.Contracts.Domain;

/// <summary>
/// The user data stream <c>ORDER_TRADE_UPDATE</c> event, raised whenever an order is created, changes state, or
/// receives a fill. <see cref="CreatedAt"/> is only known to be accurate when <see cref="Status"/> is
/// <see cref="OrderStatus.New"/> (the event that announces the order); for later events it is left at zero
/// since Binance does not repeat the original creation time.
/// </summary>
/// <param name="TradeId">The id of the trade that triggered this event, or empty if it was not fill-triggered.</param>
/// <param name="OrderId">The exchange-assigned order id.</param>
/// <param name="ClientOrderId">The client-supplied order id.</param>
/// <param name="Range">The position side the order acts on (long/short in hedge mode, or both in one-way mode).</param>
/// <param name="Symbol">The instrument symbol.</param>
/// <param name="Type">The order type.</param>
/// <param name="Side">The order side (buy/sell).</param>
/// <param name="TotalQty">The order's original quantity.</param>
/// <param name="Price">The order's limit price.</param>
/// <param name="LevelPrice">The order's trigger/stop price, for conditional order types.</param>
/// <param name="ReduceOnly">Whether the order may only reduce an existing position, never open or flip it.</param>
/// <param name="Status">The order's status after this event.</param>
/// <param name="ExecutedQty">The cumulative filled quantity.</param>
/// <param name="ExecutedPrice">The average fill price across all fills so far.</param>
/// <param name="LastExecutedQty">The quantity filled by the trade that triggered this event.</param>
/// <param name="LastExecutedPrice">The price of the trade that triggered this event.</param>
/// <param name="CommissionAmount">The commission charged for the triggering trade.</param>
/// <param name="CommissionAsset">The asset the commission was charged in.</param>
/// <param name="IsMaker">Whether the triggering trade filled on the maker side.</param>
/// <param name="CreatedAt">The order's creation time, known only when <see cref="Status"/> is <see cref="OrderStatus.New"/>.</param>
/// <param name="UpdatedAt">The timestamp of this event, in Unix milliseconds.</param>
internal sealed record OrderUpdateEvent(
    string TradeId,
    string OrderId,
    string ClientOrderId,
    OrientationRange Range,
    string Symbol,
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
    long CreatedAt,
    long UpdatedAt
);
