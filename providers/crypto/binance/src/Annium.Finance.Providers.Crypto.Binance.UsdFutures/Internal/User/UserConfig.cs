using Annium.Finance.Providers.Core.Shared.Loaders;
using Annium.Finance.Providers.Crypto.Binance.Base.User;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.User;

/// <summary>
/// Resolved user connector configuration (endpoints, credentials, listen key path) for USD-M futures, plus the
/// reload schedules for account context, orders and trades.
/// </summary>
internal sealed record UserConfig : UserConfigBase
{
    /// <summary>The reload schedule for the account context (assets and positions) loader.</summary>
    public required CompositeLoaderConfig ReloadContext { get; init; }

    /// <summary>The reload schedule for the open orders loader.</summary>
    public required CompositeLoaderConfig ReloadOrders { get; init; }

    /// <summary>The reload schedule for the trades loader.</summary>
    public required CompositeLoaderConfig ReloadTrades { get; init; }
}
