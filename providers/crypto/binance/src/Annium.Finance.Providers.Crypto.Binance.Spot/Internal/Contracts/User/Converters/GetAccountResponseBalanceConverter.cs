using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Annium.Finance.Providers.Abstractions.Domain.Dto;
using Annium.Serialization.Json;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Internal.Contracts.User.Converters;

internal class GetAccountResponseBalanceConverter : JsonConverter<AssetDto>
{
    public override AssetDto Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("deserialization failed");

        var currentDepth = reader.CurrentDepth;

        var asset = string.Empty;
        var free = 0m;
        var locked = 0m;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject && reader.CurrentDepth == currentDepth)
            {
                var balance = new AssetDto(asset, free, locked);

                return balance;
            }

            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                var propertyName = reader.GetString();

                reader.Read();

                switch (propertyName)
                {
                    case "asset":
                        asset = reader.GetString().NotNull();
                        break;
                    case "free":
                        free = reader.GetDecimalFromString();
                        break;
                    case "locked":
                        locked = reader.GetDecimalFromString();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }
        }

        throw new JsonException("Unexpected end of json");
    }

    public override void Write(Utf8JsonWriter writer, AssetDto value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
