namespace Annium.Finance.Providers.Crypto.Binance.Spot;

public static class Constants
{
    public const string Provider = "binance.spot";
    internal const string ExchangeInfoSerializerKey = $"{Provider}_{nameof(ExchangeInfoSerializerKey)}";
    internal const string InstrumentTickerSerializerKey = $"{Provider}_{nameof(InstrumentTickerSerializerKey)}";
}
