using System.Text.Json;
using Annium.Finance.Providers.Crypto.Binance.Base.Shared.Contracts.Converters;
using Annium.Serialization.Json;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Shared.Contracts;

internal static class SharedContracts
{
    public static JsonSerializerOptions ServerTime { get; } =
        new JsonSerializerOptions()
            .ResetConverters()
            .AddConverter<ServerTimeConverter>()
            .AddConverter<OperationResultConverter>();
}
