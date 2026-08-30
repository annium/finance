using System.Text.Json;
using Annium.Finance.Providers.Crypto.Binance.Base.Shared.Contracts.Converters;
using Annium.Serialization.Json;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Shared.Contracts;

/// <summary>
/// JSON serializer options for endpoints shared across market and user data, currently just server time sync.
/// </summary>
internal static class SharedContracts
{
    /// <summary>Serializer options for the server time endpoint.</summary>
    public static JsonSerializerOptions ServerTime { get; } =
        new JsonSerializerOptions()
            .ResetConverters()
            .AddConverter<ServerTimeConverter>()
            .AddConverter<OperationResultConverter>();
}
