using System.Text.Json;
using Annium.Finance.Providers.Crypto.Binance.Base.Shared.Contracts.Converters;
using Annium.Finance.Providers.Crypto.Binance.Base.User.Contracts.Converters;
using Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.User.Contracts.Converters;
using Annium.Serialization.Json;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.User.Contracts;

internal static class UserContracts
{
    public static JsonSerializerOptions GetAccount { get; } =
        new JsonSerializerOptions()
            .ResetConverters()
            .AddConverter<GetAccountResponseConverter>()
            .AddConverter<GetAccountResponseBalanceConverter>()
            .AddConverter<GetAccountResponsePositionConverter>()
            .AddConverter<OperationResultConverter>();

    public static JsonSerializerOptions GetOrder { get; } =
        new JsonSerializerOptions()
            .ResetConverters()
            .AddConverter<GetOrderResponseConverter>()
            .AddConverter<OperationResultConverter>();

    public static JsonSerializerOptions GetTrade { get; } =
        new JsonSerializerOptions()
            .ResetConverters()
            .AddConverter<GetTradeResponseConverter>()
            .AddConverter<OperationResultConverter>();

    public static JsonSerializerOptions SetLeverage { get; } =
        new JsonSerializerOptions()
            .ResetConverters()
            .AddConverter<LeverageResponseConverter>()
            .AddConverter<OperationResultConverter>();

    public static JsonSerializerOptions InitOrder { get; } =
        new JsonSerializerOptions()
            .ResetConverters()
            .AddConverter<InitOrderResponseConverter>()
            .AddConverter<OperationResultConverter>();

    public static JsonSerializerOptions ModifyOrder { get; } =
        new JsonSerializerOptions()
            .ResetConverters()
            .AddConverter<InitOrderResponseConverter>()
            .AddConverter<OperationResultConverter>();

    public static JsonSerializerOptions CancelOrder { get; } =
        new JsonSerializerOptions()
            .ResetConverters()
            .AddConverter<CancelOrderResponseConverter>()
            .AddConverter<OperationResultConverter>();

    public static JsonSerializerOptions CancelAllOrders { get; } =
        new JsonSerializerOptions().ResetConverters().AddConverter<OperationResultConverter>();

    public static JsonSerializerOptions ListenKey { get; } =
        new JsonSerializerOptions()
            .ResetConverters()
            .AddConverter<ListenKeyResponseConverter>()
            .AddConverter<OperationResultConverter>();

    public static JsonSerializerOptions AccountConfigurationUpdate { get; } =
        new JsonSerializerOptions().ResetConverters().AddConverter<AccountConfigUpdateEventConverter>();

    public static JsonSerializerOptions BalanceAndPositionUpdate { get; } =
        new JsonSerializerOptions()
            .ResetConverters()
            .AddConverter<BalanceAndPositionUpdateEventConverter>()
            .AddConverter<BalanceAndPositionUpdateEventBalanceConverter>()
            .AddConverter<BalanceAndPositionUpdateEventPositionConverter>()
            .AddConverter<OperationResultConverter>();

    public static JsonSerializerOptions OrderUpdate { get; } =
        new JsonSerializerOptions()
            .ResetConverters()
            .AddConverter<OrderUpdateEventConverter>()
            .AddConverter<OperationResultConverter>();
}
