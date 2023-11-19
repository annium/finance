using System.Text.Json;
using Annium.Core.DependencyInjection;
using Annium.Finance.Providers.Crypto.Binance.Base.Contracts.Shared.Converters;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Contracts.User;

internal class Contracts
{
    public JsonSerializerOptions InitOrder { get; } =
        new JsonSerializerOptions().ResetConverters().AddConverter<OperationResultConverter>();

    public JsonSerializerOptions ModifyOrder { get; } =
        new JsonSerializerOptions().ResetConverters().AddConverter<OperationResultConverter>();

    public JsonSerializerOptions CancelOrder { get; } =
        new JsonSerializerOptions().ResetConverters().AddConverter<OperationResultConverter>();

    public JsonSerializerOptions CancelAllOrders { get; } =
        new JsonSerializerOptions().ResetConverters().AddConverter<OperationResultConverter>();
}
