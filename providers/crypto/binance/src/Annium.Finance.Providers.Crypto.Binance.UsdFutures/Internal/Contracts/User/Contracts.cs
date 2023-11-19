using System.Text.Json;
using Annium.Core.DependencyInjection;
using Annium.Finance.Providers.Crypto.Binance.Base.Contracts.Shared.Converters;
using Annium.Finance.Providers.Crypto.Binance.Base.Contracts.User.Converters;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Contracts.User;

internal class Contracts
{
    public JsonSerializerOptions ListenKey { get; } =
        new JsonSerializerOptions().ResetConverters().AddConverter<ListenKeyResponseConverter>();

    public JsonSerializerOptions InitOrder { get; } =
        new JsonSerializerOptions().ResetConverters().AddConverter<OperationResultConverter>();

    public JsonSerializerOptions ModifyOrder { get; } =
        new JsonSerializerOptions().ResetConverters().AddConverter<OperationResultConverter>();

    public JsonSerializerOptions CancelOrder { get; } =
        new JsonSerializerOptions().ResetConverters().AddConverter<OperationResultConverter>();

    public JsonSerializerOptions CancelAllOrders { get; } =
        new JsonSerializerOptions().ResetConverters().AddConverter<OperationResultConverter>();
}
