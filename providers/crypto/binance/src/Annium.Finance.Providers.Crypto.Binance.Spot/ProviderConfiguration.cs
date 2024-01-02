using Annium.Finance.Providers.Crypto.Binance.Base;
using Annium.Finance.Providers.Shared.Loaders;
using Annium.Finance.Providers.Shared.Services;

namespace Annium.Finance.Providers.Crypto.Binance.Spot;

public sealed record ProviderConfiguration
{
    public ListenKeyConfiguration ListenKey { get; init; } = new();
    public ServerTimeWatcherConfig ServerTime { get; init; } = new(2_000, 5_000);
    public CompositeLoaderConfig ReloadAccount { get; init; } = new(3_000, 5, 5_000, 20_000, 5_000);
    public CompositeLoaderConfig ReloadOrders { get; init; } = new(3_000, 5, 10_000, 60_000, 5_000);
    public CompositeLoaderConfig ReloadTrades { get; init; } = new(5_000, 5, 10_000, 0, 5_000);
}
