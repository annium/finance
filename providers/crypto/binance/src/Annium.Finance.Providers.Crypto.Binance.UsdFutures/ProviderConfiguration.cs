namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures;

public sealed record ProviderConfiguration
{
    public int ReloadAccountInterval { get; init; } = 20_000;
    public int ReloadAccountDebounce { get; init; } = 5_000;
    public int ReloadOrdersInterval { get; init; } = 60_000;
    public int ReloadOrdersDebounce { get; init; } = 5_000;
    public int ReloadDealsDebounce { get; init; } = 5_000;
}
