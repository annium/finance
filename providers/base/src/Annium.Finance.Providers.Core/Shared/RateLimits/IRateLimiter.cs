using System;

namespace Annium.Finance.Providers.Core.Shared.RateLimits;

/// <summary>
/// Tracks a provider's rate-limit weight usage against a configured limit, so callers can check whether a
/// request is safe to send before making it.
/// </summary>
public interface IRateLimiter : IDisposable
{
    /// <summary>
    /// Checks whether the currently used weight is still under the rate limit's water mark.
    /// </summary>
    /// <returns><see langword="true"/> if a request can be executed now; otherwise, <see langword="false"/>.</returns>
    bool CanExecute();

    /// <summary>
    /// Updates the rate limit and recalculates the water mark below which <see cref="CanExecute"/> allows requests.
    /// </summary>
    /// <param name="limit">The new rate limit.</param>
    void UpdateLimit(int limit);

    /// <summary>
    /// Reports the weight currently used, as returned by the provider (e.g. from a response header), replacing
    /// any previously reported value.
    /// </summary>
    /// <param name="weight">The currently used weight.</param>
    void UsedWeight(int weight);
}
