namespace Annium.Finance.Providers.Core.Shared.TimeSync;

/// <summary>
/// Provides the provider's current server time, kept in sync in the background and extrapolated between
/// refreshes.
/// </summary>
public interface IServerTimeSource
{
    /// <summary>Gets the current server time, as Unix milliseconds, extrapolated from the last successful refresh.</summary>
    long ServerTime { get; }
}
