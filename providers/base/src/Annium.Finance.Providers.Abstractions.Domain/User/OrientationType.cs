using Annium.Core.Mapper.Attributes;

namespace Annium.Finance.Providers.Abstractions.Domain.User;

/// <summary>
/// Identifies the directional stance of a position.
/// </summary>
[AutoMapped]
public enum OrientationType
{
    /// <summary>The position profits when the price rises; opened by buying, closed by selling.</summary>
    Long,

    /// <summary>The position profits when the price falls; opened by selling, closed by buying.</summary>
    Short,
}
