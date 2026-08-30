using Annium.Core.Mapper.Attributes;

namespace Annium.Finance.Providers.Abstractions.Domain.User;

/// <summary>
/// Represents the lifecycle status of an order.
/// </summary>
[AutoMapped]
public enum OrderStatus
{
    /// <summary>The order has been accepted and is open, with no quantity filled yet.</summary>
    New,

    /// <summary>The order is open and part of its quantity has been filled.</summary>
    PartiallyFilled,

    /// <summary>The order's entire quantity has been filled.</summary>
    Filled,

    /// <summary>The order was canceled before being fully filled.</summary>
    Canceled,

    /// <summary>The order was rejected by the provider and never became active.</summary>
    Rejected,

    /// <summary>The order's time in force elapsed before it could be fully filled.</summary>
    Expired,
}
