namespace Annium.Finance.Providers.Shared.Services;

public sealed record SnapshotLoaderConfig(int FastInterval, int SlowInterval, int FastRequestsLimit);
