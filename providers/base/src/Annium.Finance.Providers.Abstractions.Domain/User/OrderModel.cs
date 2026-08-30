namespace Annium.Finance.Providers.Abstractions.Domain.User;

/// <summary>
/// Represents an order as reported by a provider, combining the terms it was placed with and its current execution progress.
/// </summary>
/// <param name="Id">The provider-assigned identifier of the order.</param>
/// <param name="ClientOrderId">The client-assigned identifier the order was placed under.</param>
/// <param name="Range">The orientation range the order is restricted to opening or closing within.</param>
/// <param name="Symbol">The instrument symbol the order was placed for.</param>
/// <param name="Side">The side (buy or sell) the order was placed on.</param>
/// <param name="Type">The type of the order.</param>
/// <param name="TotalQty">The total quantity the order was placed for, in the instrument's base asset.</param>
/// <param name="Price">The limit price of the order; zero for market and stop/take-profit market orders.</param>
/// <param name="LevelPrice">The trigger price of a stop/take-profit order; zero for orders that are not leveled.</param>
/// <param name="ReduceOnly">Whether the order may only reduce an existing position, never open or extend one.</param>
/// <param name="CreatedAt">The Unix timestamp, in milliseconds, at which the order was placed.</param>
/// <param name="Status">The current lifecycle status of the order.</param>
/// <param name="ExecutedQty">The quantity filled so far, in the instrument's base asset.</param>
/// <param name="ExecutedPrice">The volume-weighted average price the order has been filled at so far.</param>
/// <param name="UpdatedAt">The Unix timestamp, in milliseconds, at which the order was last updated.</param>
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
