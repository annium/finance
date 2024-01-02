using Annium.Finance.Providers.Crypto.Binance.Base;
using Annium.Finance.Providers.Shared.Loaders;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Internal;

internal sealed record UserConfig : UserConfigBase
{
    public required CompositeLoaderConfig ReloadContext { get; init; }
    public required CompositeLoaderConfig ReloadOrders { get; init; }
    public required CompositeLoaderConfig ReloadTrades { get; init; }
}
