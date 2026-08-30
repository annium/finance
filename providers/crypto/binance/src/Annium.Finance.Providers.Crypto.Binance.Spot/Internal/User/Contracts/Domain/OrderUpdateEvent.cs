using Annium.Finance.Providers.Abstractions.Domain.User;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Internal.User.Contracts.Domain;

/// <summary>
/// A Binance user data stream <c>executionReport</c> event, reporting an order's terms together with its
/// execution progress as of the triggering update.
/// </summary>
/// <param name="Symbol">The instrument symbol the order was placed for.</param>
/// <param name="TradeId">The identifier of the trade that triggered this update, if any.</param>
/// <param name="OrderId">The provider-assigned identifier of the order.</param>
/// <param name="ClientOrderId">The client-assigned identifier the order was placed under.</param>
/// <param name="Range">The orientation range the order is restricted to; always <see cref="OrientationRange.Both"/> on spot.</param>
/// <param name="Type">The type of the order.</param>
/// <param name="Side">The side (buy or sell) the order was placed on.</param>
/// <param name="TotalQty">The total quantity the order was placed for, in the instrument's base asset.</param>
/// <param name="Price">The limit price of the order; zero for market and stop/take-profit market orders.</param>
/// <param name="LevelPrice">The trigger price of a stop/take-profit order; zero for orders that are not leveled.</param>
/// <param name="Status">The current lifecycle status of the order.</param>
/// <param name="ExecutedQty">The cumulative quantity filled so far, in the instrument's base asset.</param>
/// <param name="ExecutedPrice">The volume-weighted average price the order has been filled at so far.</param>
/// <param name="LastExecutedQty">The quantity filled by the trade that triggered this update.</param>
/// <param name="LastExecutedPrice">The price of the trade that triggered this update.</param>
/// <param name="CommissionAmount">The commission charged for the last execution.</param>
/// <param name="CommissionAsset">The asset the commission was charged in.</param>
/// <param name="IsMaker">Whether the last execution filled as a maker order.</param>
/// <param name="CreatedAt">The Unix timestamp, in milliseconds, at which the order was placed.</param>
/// <param name="UpdatedAt">The Unix timestamp, in milliseconds, at which this update was generated.</param>
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
