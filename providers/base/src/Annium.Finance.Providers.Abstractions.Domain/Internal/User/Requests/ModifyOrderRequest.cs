using Annium.Finance.Providers.Abstractions.Domain.User;
using Annium.Finance.Providers.Abstractions.Domain.User.Requests;

namespace Annium.Finance.Providers.Abstractions.Domain.Internal.User.Requests;

/// <summary>
/// Default implementation of <see cref="IModifyOrderRequest"/>, built by <see cref="RequestBuilder"/>.
/// </summary>
internal sealed record ModifyOrderRequest : IModifyOrderRequest
{
    /// <summary>Gets the existing order being modified.</summary>
    public required OrderModel Order { get; init; }

    /// <summary>Gets the side (buy or sell) the modified order should have.</summary>
    public required OrderSide Side { get; init; }

    /// <summary>Gets the type the modified order should have.</summary>
    public required OrderType Type { get; init; }

    /// <summary>Gets the quantity the modified order should have, in the instrument's base asset.</summary>
    public required decimal Qty { get; init; }

    /// <summary>Gets the limit price the modified order should have; zero for market and stop/take-profit market orders.</summary>
    public required decimal Price { get; init; }

    /// <summary>Gets the trigger price the modified order should have; zero for orders that are not leveled.</summary>
    public required decimal LevelPrice { get; init; }
}
