namespace Annium.Finance.Providers.Core.Shared.Loaders;

public record SnapshotLoaderConfig(int FastInterval, int FastRequestsLimit, int SlowInterval);
