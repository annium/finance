namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures;

public static class Constants
{
    public const string Provider = "binance.usdfutures";
    internal const string ExchangeInfoSerializerKey = $"{Provider}_{nameof(ExchangeInfoSerializerKey)}";
    internal const string InstrumentTickerSerializerKey = $"{Provider}_{nameof(InstrumentTickerSerializerKey)}";
}
