namespace Annium.Finance.Providers.Abstractions.Domain.User;

/// <summary>
/// Represents a leveraged position held on an instrument.
/// </summary>
public interface IPosition
{
    /// <summary>Gets the leverage multiplier applied to the position (e.g. 10 for 10x leverage).</summary>
    decimal Leverage { get; }
}
