namespace Annium.Finance.Providers.Abstractions.Domain.User.Requests;

/// <summary>
/// Represents a request to modify an existing order.
/// </summary>
public interface IModifyOrderRequest
{
    /// <summary>Gets the existing order being modified.</summary>
    OrderModel Order { get; }

    /// <summary>Gets the side (buy or sell) the modified order should have.</summary>
    OrderSide Side { get; }

    /// <summary>Gets the type the modified order should have.</summary>
    OrderType Type { get; }

    /// <summary>Gets the quantity the modified order should have, in the instrument's base asset.</summary>
    decimal Qty { get; }

    /// <summary>Gets the limit price the modified order should have; zero for market and stop/take-profit market orders.</summary>
    decimal Price { get; }

    /// <summary>Gets the trigger price the modified order should have; zero for orders that are not leveled.</summary>
    decimal LevelPrice { get; }
}
