namespace Annium.Finance.Providers.Shared.Loaders;

public sealed record CompositeLoaderConfig(
    int FastInterval,
    int FastRequestsLimit,
    int SlowInterval,
    int Interval,
    int Debounce
) : SnapshotLoaderConfig(FastInterval, FastRequestsLimit, SlowInterval);
