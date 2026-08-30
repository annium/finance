namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures;

/// <summary>
/// Provider key and per-endpoint registration keys for the Binance USD-M futures provider, used to register and
/// resolve the <see cref="Annium.Net.Http.IHttpRequestFactory"/> instances backing each endpoint.
/// </summary>
public static class Constants
{
    /// <summary>The provider key this integration registers itself under.</summary>
    public const string Provider = "binance.usdfutures";

    // market
    /// <summary>Registration key for the exchange info (symbols, filters, rate limits) endpoint.</summary>
    internal const string ExchangeInfoKey = $"{Provider}_{nameof(ExchangeInfoKey)}";

    /// <summary>Registration key for the historical klines/candles endpoint.</summary>
    internal const string CandleKey = $"{Provider}_{nameof(CandleKey)}";

    /// <summary>Registration key for the book ticker (best bid/ask) stream.</summary>
    internal const string InstrumentTickerKey = $"{Provider}_{nameof(InstrumentTickerKey)}";

    /// <summary>Registration key for the server time endpoint.</summary>
    internal const string ServerTimeKey = $"{Provider}_{nameof(ServerTimeKey)}";

    // user data load
    /// <summary>Registration key for the account info (balances and positions) endpoint.</summary>
    internal const string GetAccountKey = $"{Provider}_{nameof(GetAccountKey)}";

    /// <summary>Registration key for the order lookup endpoint.</summary>
    internal const string GetOrderKey = $"{Provider}_{nameof(GetOrderKey)}";

    /// <summary>Registration key for the trade (user fills) lookup endpoint.</summary>
    internal const string GetTradeKey = $"{Provider}_{nameof(GetTradeKey)}";

    // user data trade
    /// <summary>Registration key for the change-leverage endpoint.</summary>
    internal const string SetLeverageKey = $"{Provider}_{nameof(SetLeverageKey)}";

    /// <summary>Registration key for the place-order endpoint.</summary>
    internal const string InitOrderKey = $"{Provider}_{nameof(InitOrderKey)}";

    /// <summary>Registration key for the modify-order endpoint.</summary>
    internal const string ModifyOrderKey = $"{Provider}_{nameof(ModifyOrderKey)}";

    /// <summary>Registration key for the cancel-order endpoint.</summary>
    internal const string CancelOrderKey = $"{Provider}_{nameof(CancelOrderKey)}";

    /// <summary>Registration key for the cancel-all-orders endpoint.</summary>
    internal const string CancelAllOrdersKey = $"{Provider}_{nameof(CancelAllOrdersKey)}";

    // user data updates
    /// <summary>Registration key for the listen key (user data stream token) endpoints.</summary>
    internal const string ListenKeyKey = $"{Provider}_{nameof(ListenKeyKey)}";

    /// <summary>Registration key for the <c>ACCOUNT_CONFIG_UPDATE</c> user data stream event.</summary>
    internal const string AccountConfigurationUpdateKey = $"{Provider}_{nameof(AccountConfigurationUpdateKey)}";

    /// <summary>Registration key for the <c>ACCOUNT_UPDATE</c> user data stream event.</summary>
    internal const string BalanceAndPositionUpdateKey = $"{Provider}_{nameof(BalanceAndPositionUpdateKey)}";

    /// <summary>Registration key for the <c>ORDER_TRADE_UPDATE</c> user data stream event.</summary>
    internal const string OrderUpdateKey = $"{Provider}_{nameof(OrderUpdateKey)}";
}
