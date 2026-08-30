using System;
using Annium.Core.Mapper.Attributes;

namespace Annium.Finance.Providers.Abstractions.Domain.User;

/// <summary>
/// Represents the lifecycle state of a position, derived from the running quantities its opening and closing orders have accumulated. See <see cref="Helpers.PositionHelper.ResolveState{T}"/>.
/// </summary>
[AutoMapped]
[Flags]
public enum PositionState
{
    /// <summary>No orders have been placed yet; the position has no size and no history.</summary>
    Blank = 1 << 0,

    /// <summary>An opening order is currently unfilled or partially filled.</summary>
    Opening = 1 << 1,

    /// <summary>An opening order has been filled, contributing to the position's size.</summary>
    Opened = 1 << 2,

    /// <summary>A closing order is currently unfilled or partially filled.</summary>
    Closing = 1 << 3,

    /// <summary>A closing order has been filled, reducing the position's size.</summary>
    Closed = 1 << 4,

    /// <summary>The position was fully opened and then fully closed.</summary>
    Filled = 1 << 5,

    /// <summary>All orders for the position were canceled or rejected before opening it.</summary>
    Canceled = 1 << 6,
}
