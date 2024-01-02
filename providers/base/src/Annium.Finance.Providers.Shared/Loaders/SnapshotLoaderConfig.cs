namespace Annium.Finance.Providers.Shared.Loaders;

public record SnapshotLoaderConfig(int FastInterval, int FastRequestsLimit, int SlowInterval);
