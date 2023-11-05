namespace Annium.Finance.Providers.Crypto.Binance.Spot;

public static class Constants
{
    public const string Provider = "binance.spot";
    internal const string ExchangeInfoKey = $"{Provider}_{nameof(ExchangeInfoKey)}";
    internal const string InstrumentTickerKey = $"{Provider}_{nameof(InstrumentTickerKey)}";
}
