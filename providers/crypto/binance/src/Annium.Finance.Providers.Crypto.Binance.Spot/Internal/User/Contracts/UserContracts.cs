using System.Text.Json;
using Annium.Finance.Providers.Crypto.Binance.Base.Shared.Contracts.Converters;
using Annium.Finance.Providers.Crypto.Binance.Base.User.Contracts.Converters;
using Annium.Finance.Providers.Crypto.Binance.Spot.Internal.User.Contracts.Converters;
using Annium.Serialization.Json;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Internal.User.Contracts;

/// <summary>Preconfigured JSON serializer options for each Binance spot account, trading and user data stream endpoint.</summary>
internal static class UserContracts
{
    /// <summary>Serializer options for the get-account (balances) endpoint.</summary>
    public static JsonSerializerOptions GetAccount { get; } =
        new JsonSerializerOptions()
            .ResetConverters()
            .AddConverter<GetAccountResponseConverter>()
            .AddConverter<GetAccountResponseBalanceConverter>()
            .AddConverter<OperationResultConverter>();

    /// <summary>Serializer options for the get-order endpoint.</summary>
    public static JsonSerializerOptions GetOrder { get; } =
        new JsonSerializerOptions()
            .ResetConverters()
            .AddConverter<GetOrderResponseConverter>()
            .AddConverter<OperationResultConverter>();

    /// <summary>Serializer options for the get-trade (my trades) endpoint.</summary>
    public static JsonSerializerOptions GetTrade { get; } =
        new JsonSerializerOptions()
            .ResetConverters()
            .AddConverter<GetTradeResponseConverter>()
            .AddConverter<OperationResultConverter>();

    /// <summary>Serializer options for the new-order (place order) endpoint.</summary>
    public static JsonSerializerOptions InitOrder { get; } =
        new JsonSerializerOptions()
            .ResetConverters()
            .AddConverter<InitOrderResponseConverter>()
            .AddConverter<OperationResultConverter>();

    /// <summary>Serializer options for the cancel-replace (modify order) endpoint, covering both the success and failure response shapes.</summary>
    public static JsonSerializerOptions ModifyOrder { get; } =
        new JsonSerializerOptions()
            .ResetConverters()
            .AddConverter<ModifyOrderSuccessResponseConverter>()
            .AddConverter<ModifyOrderFailureResponseConverter>()
            .AddConverter<OperationResultConverter>();

    /// <summary>Serializer options for the cancel-order endpoint.</summary>
    public static JsonSerializerOptions CancelOrder { get; } =
        new JsonSerializerOptions()
            .ResetConverters()
            .AddConverter<CancelOrderResponseConverter>()
            .AddConverter<OperationResultConverter>();

    /// <summary>Serializer options for the cancel-all-open-orders endpoint.</summary>
    public static JsonSerializerOptions CancelAllOrders { get; } =
        new JsonSerializerOptions().ResetConverters().AddConverter<OperationResultConverter>();

    /// <summary>Serializer options for the listen key (user data stream token) endpoint.</summary>
    public static JsonSerializerOptions ListenKey { get; } =
        new JsonSerializerOptions()
            .ResetConverters()
            .AddConverter<ListenKeyResponseConverter>()
            .AddConverter<OperationResultConverter>();

    /// <summary>Serializer options for the <c>outboundAccountPosition</c> user data stream event.</summary>
    public static JsonSerializerOptions AccountUpdate { get; } =
        new JsonSerializerOptions()
            .ResetConverters()
            .AddConverter<AccountUpdateEventConverter>()
            .AddConverter<AccountUpdateEventBalanceConverter>()
            .AddConverter<OperationResultConverter>();

    /// <summary>Serializer options for the <c>executionReport</c> user data stream event.</summary>
    public static JsonSerializerOptions OrderUpdate { get; } =
        new JsonSerializerOptions()
            .ResetConverters()
            .AddConverter<OrderUpdateEventConverter>()
            .AddConverter<OperationResultConverter>();
}
