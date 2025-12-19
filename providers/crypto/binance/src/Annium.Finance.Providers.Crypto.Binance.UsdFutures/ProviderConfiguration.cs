using Annium.Finance.Providers.Core.Shared.Loaders;
using Annium.Finance.Providers.Core.Shared.ServerTime;
using Annium.Finance.Providers.Crypto.Binance.Base;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures;

public sealed record ProviderConfiguration
{
    public ListenKeyConfiguration ListenKey { get; init; } = new(5_000, 60_000);
    public ServerTimeProviderConfig ServerTime { get; init; } = new(2_000, 5_000);
    public CompositeLoaderConfig ReloadContext { get; init; } = new(1_000, 5, 3_000, 5_000, 5_000);
    public CompositeLoaderConfig ReloadOrders { get; init; } = new(3_000, 5, 10_000, 60_000, 5_000);
    public CompositeLoaderConfig ReloadTrades { get; init; } = new(5_000, 5, 10_000, 0, 5_000);
}
