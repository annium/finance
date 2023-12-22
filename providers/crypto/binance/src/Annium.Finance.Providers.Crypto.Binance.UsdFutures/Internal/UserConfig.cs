using Annium.Finance.Providers.Crypto.Binance.Base;
using Annium.Finance.Providers.Shared.Services;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal;

internal sealed record UserConfig : UserConfigBase
{
    public required CompositeLoaderConfig ReloadAccount { get; init; }
    public required CompositeLoaderConfig ReloadOrders { get; init; }
    public required CompositeLoaderConfig ReloadTrades { get; init; }
}
