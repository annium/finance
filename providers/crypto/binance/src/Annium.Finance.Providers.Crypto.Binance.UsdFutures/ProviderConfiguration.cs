using Annium.Finance.Providers.Core.Shared.Loaders;
using Annium.Finance.Providers.Core.Shared.TimeSync;
using Annium.Finance.Providers.Crypto.Binance.Base.User.Services;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures;

/// <summary>
/// User-facing configuration for the Binance USD-M futures provider: listen key keep-alive, server time sync,
/// and the reload schedules for account context, orders and trades. Passed to
/// <see cref="ProviderRegistrationContextExtensions"/>'s registration method; defaults are used when registering
/// without an explicit configuration.
/// </summary>
public sealed record ProviderConfiguration
{
    /// <summary>The listen key ping interval and expiration handling.</summary>
    public ListenKeyConfiguration ListenKey { get; init; } = new(5_000, 60_000);

    /// <summary>The server time sync interval and staleness tolerance.</summary>
    public ServerTimeProviderConfig ServerTime { get; init; } = new(2_000, 5_000);

    /// <summary>The reload schedule for the account context (assets and positions) loader.</summary>
    public CompositeLoaderConfig ReloadContext { get; init; } = new(1_000, 5, 3_000, 5_000, 5_000);

    /// <summary>The reload schedule for the open orders loader.</summary>
    public CompositeLoaderConfig ReloadOrders { get; init; } = new(3_000, 5, 10_000, 60_000, 5_000);

    /// <summary>The reload schedule for the trades loader.</summary>
    public CompositeLoaderConfig ReloadTrades { get; init; } = new(5_000, 5, 10_000, 0, 5_000);
}
