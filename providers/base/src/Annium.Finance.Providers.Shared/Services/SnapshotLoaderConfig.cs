namespace Annium.Finance.Providers.Shared.Services;

public record SnapshotLoaderConfig(int FastInterval, int FastRequestsLimit, int SlowInterval);
