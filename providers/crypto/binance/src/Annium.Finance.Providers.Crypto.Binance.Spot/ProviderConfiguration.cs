using Annium.Finance.Providers.Core.Shared.Loaders;
using Annium.Finance.Providers.Core.Shared.TimeSync;
using Annium.Finance.Providers.Crypto.Binance.Base.User.Services;

namespace Annium.Finance.Providers.Crypto.Binance.Spot;

/// <summary>User-configurable timing settings for the Binance spot provider, with defaults suited to typical usage.</summary>
public sealed record ProviderConfiguration
{
    /// <summary>Gets the keep-alive timing for the user data stream listen key.</summary>
    public ListenKeyConfiguration ListenKey { get; init; } = new(5_000, 60_000);

    /// <summary>Gets the polling/timeout settings for the server time sync.</summary>
    public ServerTimeProviderConfig ServerTime { get; init; } = new(2_000, 5_000);

    /// <summary>Gets the schedule for reloading account balances.</summary>
    public CompositeLoaderConfig ReloadContext { get; init; } = new(1_000, 5, 3_000, 5_000, 5_000);

    /// <summary>Gets the schedule for reloading open orders.</summary>
    public CompositeLoaderConfig ReloadOrders { get; init; } = new(3_000, 5, 10_000, 60_000, 5_000);

    /// <summary>Gets the schedule for reloading recent trades.</summary>
    public CompositeLoaderConfig ReloadTrades { get; init; } = new(5_000, 5, 10_000, 0, 5_000);
}
