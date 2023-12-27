using System.Text.Json;
using Annium.Core.DependencyInjection;
using Annium.Finance.Providers.Crypto.Binance.Base.Contracts.Shared.Converters;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Internal.Contracts.Shared;

internal class Contracts
{
    public JsonSerializerOptions ServerTime { get; } =
        new JsonSerializerOptions().ResetConverters().AddConverter<ServerTimeConverter>();
}
