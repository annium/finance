namespace Annium.Finance.Providers.Crypto.Binance.Spot.Internal.Contracts;

internal static class Contracts
{
    public static Market.Contracts Market { get; } = new();
    public static Shared.Contracts Shared { get; } = new();
    public static User.Contracts User { get; } = new();
}
