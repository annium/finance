using System.Text.Json;
using Annium.Finance.Providers.Crypto.Binance.Base.Shared.Contracts.Converters;
using Annium.Finance.Providers.Crypto.Binance.Base.User.Contracts.Converters;
using Annium.Finance.Providers.Crypto.Binance.Spot.Internal.Contracts.User.Converters;
using Annium.Serialization.Json;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Internal.Contracts.User;

internal class Contracts
{
    public JsonSerializerOptions GetAccount { get; } =
        new JsonSerializerOptions()
            .ResetConverters()
            .AddConverter<GetAccountResponseConverter>()
            .AddConverter<GetAccountResponseBalanceConverter>()
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

    public JsonSerializerOptions InitOrder { get; } =
        new JsonSerializerOptions()
            .ResetConverters()
            .AddConverter<InitOrderResponseConverter>()
            .AddConverter<OperationResultConverter>();

    public JsonSerializerOptions ModifyOrder { get; } =
        new JsonSerializerOptions()
            .ResetConverters()
            .AddConverter<ModifyOrderSuccessResponseConverter>()
            .AddConverter<ModifyOrderFailureResponseConverter>()
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

    public JsonSerializerOptions AccountUpdate { get; } =
        new JsonSerializerOptions()
            .ResetConverters()
            .AddConverter<AccountUpdateEventConverter>()
            .AddConverter<AccountUpdateEventBalanceConverter>()
            .AddConverter<OperationResultConverter>();

    public JsonSerializerOptions OrderUpdate { get; } =
        new JsonSerializerOptions()
            .ResetConverters()
            .AddConverter<OrderUpdateEventConverter>()
            .AddConverter<OperationResultConverter>();
}
