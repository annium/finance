namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures;

public static class Constants
{
    public const string Provider = "binance.usdfutures";
    internal const string ExchangeInfoKey = $"{Provider}_{nameof(ExchangeInfoKey)}";
    internal const string CandleKey = $"{Provider}_{nameof(CandleKey)}";
    internal const string InstrumentTickerKey = $"{Provider}_{nameof(InstrumentTickerKey)}";
}
