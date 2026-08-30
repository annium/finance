using Annium.Core.Mapper.Attributes;

namespace Annium.Finance.Providers.Abstractions.Domain.User;

/// <summary>
/// Identifies which side of the market an order is placed on.
/// </summary>
[AutoMapped]
public enum OrderSide
{
    /// <summary>The order buys the instrument's base asset.</summary>
    Buy,

    /// <summary>The order sells the instrument's base asset.</summary>
    Sell,
}
