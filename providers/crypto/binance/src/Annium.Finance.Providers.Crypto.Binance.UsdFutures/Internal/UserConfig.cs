using Annium.Finance.Providers.Core.Shared.Loaders;
using Annium.Finance.Providers.Crypto.Binance.Base;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal;

internal sealed record UserConfig : UserConfigBase
{
    public required CompositeLoaderConfig ReloadContext { get; init; }
    public required CompositeLoaderConfig ReloadOrders { get; init; }
    public required CompositeLoaderConfig ReloadTrades { get; init; }
}
