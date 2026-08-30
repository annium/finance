using System.Text.Json;
using Annium.Finance.Providers.Crypto.Binance.Base.Shared.Contracts.Converters;
using Annium.Finance.Providers.Crypto.Binance.Base.User.Contracts.Converters;
using Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.User.Contracts.Converters;
using Annium.Serialization.Json;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.User.Contracts;

/// <summary>
/// Per-endpoint JSON serializer options for the USD-M futures user data endpoints, each wiring the converters
/// needed to parse that endpoint's response or event shape.
/// </summary>
internal static class UserContracts
{
    /// <summary>Serializer options for the <c>GET /fapi/v2/account</c> endpoint.</summary>
    public static JsonSerializerOptions GetAccount { get; } =
        new JsonSerializerOptions()
            .ResetConverters()
            .AddConverter<GetAccountResponseConverter>()
            .AddConverter<GetAccountResponseBalanceConverter>()
            .AddConverter<GetAccountResponsePositionConverter>()
            .AddConverter<OperationResultConverter>();

    /// <summary>Serializer options for the order lookup endpoints (<c>GET /fapi/v1/order</c> and open/all order lists).</summary>
    public static JsonSerializerOptions GetOrder { get; } =
        new JsonSerializerOptions()
            .ResetConverters()
            .AddConverter<GetOrderResponseConverter>()
            .AddConverter<OperationResultConverter>();

    /// <summary>Serializer options for the trade (user fills) lookup endpoint (<c>GET /fapi/v1/userTrades</c>).</summary>
    public static JsonSerializerOptions GetTrade { get; } =
        new JsonSerializerOptions()
            .ResetConverters()
            .AddConverter<GetTradeResponseConverter>()
            .AddConverter<OperationResultConverter>();

    /// <summary>Serializer options for the change-leverage endpoint (<c>POST /fapi/v1/leverage</c>).</summary>
    public static JsonSerializerOptions SetLeverage { get; } =
        new JsonSerializerOptions()
            .ResetConverters()
            .AddConverter<LeverageResponseConverter>()
            .AddConverter<OperationResultConverter>();

    /// <summary>Serializer options for the place-order endpoint (<c>POST /fapi/v1/order</c>).</summary>
    public static JsonSerializerOptions InitOrder { get; } =
        new JsonSerializerOptions()
            .ResetConverters()
            .AddConverter<InitOrderResponseConverter>()
            .AddConverter<OperationResultConverter>();

    /// <summary>Serializer options for the modify-order endpoint (<c>PUT /fapi/v1/order</c>).</summary>
    public static JsonSerializerOptions ModifyOrder { get; } =
        new JsonSerializerOptions()
            .ResetConverters()
            .AddConverter<InitOrderResponseConverter>()
            .AddConverter<OperationResultConverter>();

    /// <summary>Serializer options for the cancel-order endpoint (<c>DELETE /fapi/v1/order</c>).</summary>
    public static JsonSerializerOptions CancelOrder { get; } =
        new JsonSerializerOptions()
            .ResetConverters()
            .AddConverter<CancelOrderResponseConverter>()
            .AddConverter<OperationResultConverter>();

    /// <summary>Serializer options for the cancel-all-orders endpoint (<c>DELETE /fapi/v1/allOpenOrders</c>).</summary>
    public static JsonSerializerOptions CancelAllOrders { get; } =
        new JsonSerializerOptions().ResetConverters().AddConverter<OperationResultConverter>();

    /// <summary>Serializer options for the listen key (user data stream token) endpoints.</summary>
    public static JsonSerializerOptions ListenKey { get; } =
        new JsonSerializerOptions()
            .ResetConverters()
            .AddConverter<ListenKeyResponseConverter>()
            .AddConverter<OperationResultConverter>();

    /// <summary>Serializer options for the <c>ACCOUNT_CONFIG_UPDATE</c> user data stream event.</summary>
    public static JsonSerializerOptions AccountConfigurationUpdate { get; } =
        new JsonSerializerOptions().ResetConverters().AddConverter<AccountConfigUpdateEventConverter>();

    /// <summary>Serializer options for the <c>ACCOUNT_UPDATE</c> user data stream event.</summary>
    public static JsonSerializerOptions BalanceAndPositionUpdate { get; } =
        new JsonSerializerOptions()
            .ResetConverters()
            .AddConverter<BalanceAndPositionUpdateEventConverter>()
            .AddConverter<BalanceAndPositionUpdateEventBalanceConverter>()
            .AddConverter<BalanceAndPositionUpdateEventPositionConverter>()
            .AddConverter<OperationResultConverter>();

    /// <summary>Serializer options for the <c>ORDER_TRADE_UPDATE</c> user data stream event.</summary>
    public static JsonSerializerOptions OrderUpdate { get; } =
        new JsonSerializerOptions()
            .ResetConverters()
            .AddConverter<OrderUpdateEventConverter>()
            .AddConverter<OperationResultConverter>();
}
