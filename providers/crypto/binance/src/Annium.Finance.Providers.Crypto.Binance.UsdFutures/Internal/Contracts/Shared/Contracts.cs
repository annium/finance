using System.Text.Json;
using Annium.Finance.Providers.Crypto.Binance.Base.Contracts.Shared.Converters;
using Annium.Serialization.Json;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Contracts.Shared;

internal class Contracts
{
    public JsonSerializerOptions ServerTime { get; } =
        new JsonSerializerOptions().ResetConverters().AddConverter<ServerTimeConverter>();
}
