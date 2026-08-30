namespace Annium.Finance.Providers.Core.Shared.Loaders;

/// <summary>
/// Timing configuration for an <see cref="Annium.Finance.Providers.Core.Shared.Loaders.ISnapshotLoader{T}"/>.
/// </summary>
/// <param name="FastInterval">The interval, in milliseconds, between fetches while under the fast requests limit.</param>
/// <param name="FastRequestsLimit">The number of consecutive failed fetches after which the loader switches to <see cref="SlowInterval"/>.</param>
/// <param name="SlowInterval">The interval, in milliseconds, between fetches once the fast requests limit has been reached.</param>
public record SnapshotLoaderConfig(int FastInterval, int FastRequestsLimit, int SlowInterval);
