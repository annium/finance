namespace Annium.Finance.Providers.Crypto.Binance.Spot;

public static class Constants
{
    public const string Provider = "binance.spot";
    internal const string ExchangeInfoKey = $"{Provider}_{nameof(ExchangeInfoKey)}";
    internal const string CandleKey = $"{Provider}_{nameof(CandleKey)}";
    internal const string InstrumentTickerKey = $"{Provider}_{nameof(InstrumentTickerKey)}";
    internal const string ServerTimeKey = $"{Provider}_{nameof(ServerTimeKey)}";
    internal const string InitOrderKey = $"{Provider}_{nameof(InitOrderKey)}";
    internal const string ModifyOrderKey = $"{Provider}_{nameof(ModifyOrderKey)}";
    internal const string CancelOrderKey = $"{Provider}_{nameof(CancelOrderKey)}";
    internal const string CancelAllOrdersKey = $"{Provider}_{nameof(CancelAllOrdersKey)}";
}
