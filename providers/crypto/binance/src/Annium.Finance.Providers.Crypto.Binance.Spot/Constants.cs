namespace Annium.Finance.Providers.Crypto.Binance.Spot;

/// <summary>
/// Provider name and the keys used to register and resolve the HTTP request factories and JSON serializers for
/// each Binance spot endpoint.
/// </summary>
public static class Constants
{
    /// <summary>The provider name this connector is registered under.</summary>
    public const string Provider = "binance.spot";

    // market
    /// <summary>Registration key for the exchange info (instruments and rate limits) request factory and serializer.</summary>
    internal const string ExchangeInfoKey = $"{Provider}_{nameof(ExchangeInfoKey)}";

    /// <summary>Registration key for the candle history request factory and serializer.</summary>
    internal const string CandleKey = $"{Provider}_{nameof(CandleKey)}";

    /// <summary>Registration key for the instrument ticker stream request factory and serializer.</summary>
    internal const string InstrumentTickerKey = $"{Provider}_{nameof(InstrumentTickerKey)}";

    /// <summary>Registration key for the server time request factory and serializer.</summary>
    internal const string ServerTimeKey = $"{Provider}_{nameof(ServerTimeKey)}";

    // user data load
    /// <summary>Registration key for the get-account (balances) request factory and serializer.</summary>
    internal const string GetAccountKey = $"{Provider}_{nameof(GetAccountKey)}";

    /// <summary>Registration key for the get-order request factory and serializer.</summary>
    internal const string GetOrderKey = $"{Provider}_{nameof(GetOrderKey)}";

    /// <summary>Registration key for the get-trade request factory and serializer.</summary>
    internal const string GetTradeKey = $"{Provider}_{nameof(GetTradeKey)}";

    // user data trade
    /// <summary>Registration key for the place-order request factory and serializer.</summary>
    internal const string InitOrderKey = $"{Provider}_{nameof(InitOrderKey)}";

    /// <summary>Registration key for the modify-order request factory and serializer.</summary>
    internal const string ModifyOrderKey = $"{Provider}_{nameof(ModifyOrderKey)}";

    /// <summary>Registration key for the cancel-order request factory and serializer.</summary>
    internal const string CancelOrderKey = $"{Provider}_{nameof(CancelOrderKey)}";

    /// <summary>Registration key for the cancel-all-orders request factory and serializer.</summary>
    internal const string CancelAllOrdersKey = $"{Provider}_{nameof(CancelAllOrdersKey)}";

    // user data updates
    /// <summary>Registration key for the listen key (user data stream token) request factory and serializer.</summary>
    internal const string ListenKeyKey = $"{Provider}_{nameof(ListenKeyKey)}";

    /// <summary>Registration key for the account update (<c>outboundAccountPosition</c>) user data stream event serializer.</summary>
    internal const string AccountUpdateKey = $"{Provider}_{nameof(AccountUpdateKey)}";

    /// <summary>Registration key for the order update (<c>executionReport</c>) user data stream event serializer.</summary>
    internal const string OrderUpdateKey = $"{Provider}_{nameof(OrderUpdateKey)}";
}
