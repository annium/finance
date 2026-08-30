using Annium.Finance.Providers.Core.Shared.Loaders;
using Annium.Finance.Providers.Crypto.Binance.Base.User;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Internal.User;

/// <summary>Resolved account connection settings for the Binance spot user connector.</summary>
internal sealed record UserConfig : UserConfigBase
{
    /// <summary>Gets the schedule for reloading account balances.</summary>
    public required CompositeLoaderConfig ReloadContext { get; init; }

    /// <summary>Gets the schedule for reloading open orders.</summary>
    public required CompositeLoaderConfig ReloadOrders { get; init; }

    /// <summary>Gets the schedule for reloading recent trades.</summary>
    public required CompositeLoaderConfig ReloadTrades { get; init; }
}
