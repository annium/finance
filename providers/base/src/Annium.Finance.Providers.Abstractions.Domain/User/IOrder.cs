namespace Annium.Finance.Providers.Abstractions.Domain.User;

/// <summary>
/// Represents an order placed with a provider: its terms and its execution progress so far.
/// </summary>
public interface IOrder
{
    /// <summary>Gets the side (buy or sell) the order was placed on.</summary>
    OrderSide Side { get; }

    /// <summary>Gets the type of the order.</summary>
    OrderType Type { get; }

    /// <summary>Gets the total quantity the order was placed for, in the instrument's base asset.</summary>
    decimal TotalQty { get; }

    /// <summary>Gets the limit price of the order; zero for market and stop/take-profit market orders.</summary>
    decimal Price { get; }

    /// <summary>Gets the trigger price of a stop/take-profit order; zero for orders that are not leveled.</summary>
    decimal LevelPrice { get; }

    /// <summary>Gets the current lifecycle status of the order.</summary>
    OrderStatus Status { get; }

    /// <summary>Gets the quantity filled so far, in the instrument's base asset.</summary>
    decimal ExecutedQty { get; }

    /// <summary>Gets the volume-weighted average price the order has been filled at so far.</summary>
    decimal ExecutedPrice { get; }
}
