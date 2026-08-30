namespace Annium.Finance.Providers.Abstractions.Domain.User.Requests;

/// <summary>
/// Represents a request to place a new order.
/// </summary>
public interface IInitOrderRequest
{
    /// <summary>Gets the client-assigned identifier to place the order under.</summary>
    string Id { get; }

    /// <summary>Gets the orientation range the order is restricted to opening or closing within.</summary>
    OrientationRange Range { get; }

    /// <summary>Gets the instrument symbol to place the order for.</summary>
    string Symbol { get; }

    /// <summary>Gets the side (buy or sell) to place the order on.</summary>
    OrderSide Side { get; }

    /// <summary>Gets the type of order to place.</summary>
    OrderType Type { get; }

    /// <summary>Gets the quantity to place the order for, in the instrument's base asset.</summary>
    decimal Qty { get; }

    /// <summary>Gets the limit price of the order; zero for market and stop/take-profit market orders.</summary>
    decimal Price { get; }

    /// <summary>Gets the trigger price of a stop/take-profit order; zero for orders that are not leveled.</summary>
    decimal LevelPrice { get; }

    /// <summary>Gets a value indicating whether the order may only reduce an existing position, never open or extend one.</summary>
    bool ReduceOnly { get; }
}
