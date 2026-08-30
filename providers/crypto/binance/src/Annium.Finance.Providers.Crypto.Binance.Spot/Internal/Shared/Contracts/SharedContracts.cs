using System.Text.Json;
using Annium.Finance.Providers.Crypto.Binance.Base.Shared.Contracts.Converters;
using Annium.Serialization.Json;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Internal.Shared.Contracts;

/// <summary>Preconfigured JSON serializer options for endpoints shared by the market and user data connectors.</summary>
internal static class SharedContracts
{
    /// <summary>Serializer options for the server time endpoint.</summary>
    public static JsonSerializerOptions ServerTime { get; } =
        new JsonSerializerOptions()
            .ResetConverters()
            .AddConverter<ServerTimeConverter>()
            .AddConverter<OperationResultConverter>();
}
