namespace Annium.Finance.Providers.Core.Shared.Loaders;

/// <summary>
/// Timing configuration for an <see cref="Annium.Finance.Providers.Core.Shared.Loaders.ICompositeLoader{T}"/>,
/// extending the underlying snapshot loader's configuration with the interval and debounce periods.
/// </summary>
/// <param name="FastInterval">The interval, in milliseconds, between fetches while under the fast requests limit.</param>
/// <param name="FastRequestsLimit">The number of consecutive failed fetches after which the loader switches to <see cref="SnapshotLoaderConfig.SlowInterval"/>.</param>
/// <param name="SlowInterval">The interval, in milliseconds, between fetches once the fast requests limit has been reached.</param>
/// <param name="Interval">The period, in milliseconds, between scheduled reloads while the loader is active. Zero disables interval-triggered reloads.</param>
/// <param name="Debounce">The debounce period, in milliseconds, applied to <see cref="Annium.Finance.Providers.Core.Shared.Loaders.ICompositeLoader{T}.Request"/> calls. Zero disables debounce-triggered reloads.</param>
public sealed record CompositeLoaderConfig(
    int FastInterval,
    int FastRequestsLimit,
    int SlowInterval,
    int Interval,
    int Debounce
) : SnapshotLoaderConfig(FastInterval, FastRequestsLimit, SlowInterval);
