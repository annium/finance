using Annium.Finance.Providers.Abstractions.Domain.User;
using Annium.Finance.Providers.Abstractions.Domain.User.Requests;

namespace Annium.Finance.Providers.Abstractions.Domain.Internal.User.Requests;

/// <summary>
/// Default implementation of <see cref="IInitOrderRequest"/>, built by <see cref="RequestBuilder"/> and
/// <see cref="ModifyOrderRequestExtensions.ToInitOrderRequest"/>.
/// </summary>
internal sealed record InitOrderRequest : IInitOrderRequest
{
    /// <summary>Gets the client-assigned identifier to place the order under.</summary>
    public required string Id { get; init; }

    /// <summary>Gets the orientation range the order is restricted to opening or closing within.</summary>
    public required OrientationRange Range { get; init; }

    /// <summary>Gets the instrument symbol to place the order for.</summary>
    public required string Symbol { get; init; }

    /// <summary>Gets the side (buy or sell) to place the order on.</summary>
    public required OrderSide Side { get; init; }

    /// <summary>Gets the type of order to place.</summary>
    public required OrderType Type { get; init; }

    /// <summary>Gets the quantity to place the order for, in the instrument's base asset.</summary>
    public required decimal Qty { get; init; }

    /// <summary>Gets the limit price of the order; zero for market and stop/take-profit market orders.</summary>
    public required decimal Price { get; init; }

    /// <summary>Gets the trigger price of a stop/take-profit order; zero for orders that are not leveled.</summary>
    public required decimal LevelPrice { get; init; }

    /// <summary>Gets a value indicating whether the order may only reduce an existing position, never open or extend one.</summary>
    public required bool ReduceOnly { get; init; }
}
