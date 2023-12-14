using Annium.Finance.Providers.Crypto.Binance.Base;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal;

internal sealed record UserConfig : UserConfigBase
{
    public required int ReloadAccountInterval { get; init; }
    public required int ReloadAccountDebounce { get; init; }
    public required int ReloadOrdersInterval { get; init; }
    public required int ReloadOrdersDebounce { get; init; }
    public required int ReloadDealsDebounce { get; init; }
}
