using Annium.Core.Mapper.Attributes;

namespace Annium.Finance.Providers.Abstractions.Domain.User;

/// <summary>
/// Identifies how margin is allocated to a leveraged position.
/// </summary>
[AutoMapped]
public enum MarginType
{
    /// <summary>Margin is shared across all positions in the account; a loss on one position can be covered by the whole account balance.</summary>
    Cross,

    /// <summary>Margin is ring-fenced to a single position; a loss can only consume the margin allocated to that position.</summary>
    Isolated,
}
