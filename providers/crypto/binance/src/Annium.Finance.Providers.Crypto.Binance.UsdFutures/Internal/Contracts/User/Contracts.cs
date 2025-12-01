using System.Text.Json;
using Annium.Finance.Providers.Crypto.Binance.Base.Contracts.Shared.Converters;
using Annium.Finance.Providers.Crypto.Binance.Base.Contracts.User.Converters;
using Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Contracts.User.Converters;
using Annium.Serialization.Json;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Contracts.User;

internal class Contracts
{
    public JsonSerializerOptions GetAccount { get; } =
        new JsonSerializerOptions()
            .ResetConverters()
            .AddConverter<GetAccountResponseConverter>()
            .AddConverter<GetAccountResponseBalanceConverter>()
            .AddConverter<GetAccountResponsePositionConverter>()
            .AddConverter<OperationResultConverter>();

    public JsonSerializerOptions GetOrder { get; } =
        new JsonSerializerOptions()
            .ResetConverters()
            .AddConverter<GetOrderResponseConverter>()
            .AddConverter<OperationResultConverter>();

    public JsonSerializerOptions GetTrade { get; } =
        new JsonSerializerOptions()
            .ResetConverters()
            .AddConverter<GetTradeResponseConverter>()
            .AddConverter<OperationResultConverter>();

    public JsonSerializerOptions SetLeverage { get; } =
        new JsonSerializerOptions()
            .ResetConverters()
            .AddConverter<LeverageResponseConverter>()
            .AddConverter<OperationResultConverter>();

    public JsonSerializerOptions InitOrder { get; } =
        new JsonSerializerOptions()
            .ResetConverters()
            .AddConverter<InitOrderResponseConverter>()
            .AddConverter<OperationResultConverter>();

    public JsonSerializerOptions ModifyOrder { get; } =
        new JsonSerializerOptions()
            .ResetConverters()
            .AddConverter<InitOrderResponseConverter>()
            .AddConverter<OperationResultConverter>();

    public JsonSerializerOptions CancelOrder { get; } =
        new JsonSerializerOptions()
            .ResetConverters()
            .AddConverter<CancelOrderResponseConverter>()
            .AddConverter<OperationResultConverter>();

    public JsonSerializerOptions CancelAllOrders { get; } =
        new JsonSerializerOptions().ResetConverters().AddConverter<OperationResultConverter>();

    public JsonSerializerOptions ListenKey { get; } =
        new JsonSerializerOptions()
            .ResetConverters()
            .AddConverter<ListenKeyResponseConverter>()
            .AddConverter<OperationResultConverter>();

    public JsonSerializerOptions AccountConfigurationUpdate { get; } =
        new JsonSerializerOptions().ResetConverters().AddConverter<AccountConfigUpdateEventConverter>();

    public JsonSerializerOptions BalanceAndPositionUpdate { get; } =
        new JsonSerializerOptions()
            .ResetConverters()
            .AddConverter<BalanceAndPositionUpdateEventConverter>()
            .AddConverter<BalanceAndPositionUpdateEventBalanceConverter>()
            .AddConverter<BalanceAndPositionUpdateEventPositionConverter>()
            .AddConverter<OperationResultConverter>();

    public JsonSerializerOptions OrderUpdate { get; } =
        new JsonSerializerOptions()
            .ResetConverters()
            .AddConverter<OrderUpdateEventConverter>()
            .AddConverter<OperationResultConverter>();
}
