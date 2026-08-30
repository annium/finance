using Annium.Core.Mapper.Attributes;

namespace Annium.Finance.Providers.Abstractions.Domain.User;

/// <summary>
/// Restricts which position orientations an order is allowed to open or close.
/// </summary>
[AutoMapped]
public enum OrientationRange
{
    /// <summary>The order may act on positions of either orientation.</summary>
    Both,

    /// <summary>The order may only act on long positions.</summary>
    Long,

    /// <summary>The order may only act on short positions.</summary>
    Short,
}
