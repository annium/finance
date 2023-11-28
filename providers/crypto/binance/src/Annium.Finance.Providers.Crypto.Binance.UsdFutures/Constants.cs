namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures;

public static class Constants
{
    public const string Provider = "binance.usdfutures";

    // market
    internal const string ExchangeInfoKey = $"{Provider}_{nameof(ExchangeInfoKey)}";
    internal const string CandleKey = $"{Provider}_{nameof(CandleKey)}";
    internal const string InstrumentTickerKey = $"{Provider}_{nameof(InstrumentTickerKey)}";
    internal const string ServerTimeKey = $"{Provider}_{nameof(ServerTimeKey)}";

    // user data load
    internal const string GetAccount = $"{Provider}_{nameof(GetAccount)}";
    internal const string GetOrder = $"{Provider}_{nameof(GetOrder)}";
    internal const string GetTrade = $"{Provider}_{nameof(GetTrade)}";

    // user data trade
    internal const string InitOrderKey = $"{Provider}_{nameof(InitOrderKey)}";
    internal const string ModifyOrderKey = $"{Provider}_{nameof(ModifyOrderKey)}";
    internal const string CancelOrderKey = $"{Provider}_{nameof(CancelOrderKey)}";
    internal const string CancelAllOrdersKey = $"{Provider}_{nameof(CancelAllOrdersKey)}";

    // user data updates
    internal const string ListenKeyKey = $"{Provider}_{nameof(ListenKeyKey)}";
    internal const string AccountConfigurationUpdateKey = $"{Provider}_{nameof(AccountConfigurationUpdateKey)}";
    internal const string BalanceAndPositionUpdateKey = $"{Provider}_{nameof(BalanceAndPositionUpdateKey)}";
    internal const string OrderUpdateKey = $"{Provider}_{nameof(OrderUpdateKey)}";
}
