using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Annium.Finance.Providers.Crypto.Binance.Spot.Internal.Contracts.User.Domain;
using Annium.Serialization.Json;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Internal.Contracts.User.Converters;

internal class AccountUpdateEventBalanceConverter : JsonConverter<AccountUpdateEventBalance>
{
    public override AccountUpdateEventBalance Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("Read failed");

        var currentDepth = reader.CurrentDepth;

        var asset = string.Empty;
        var free = 0m;
        var locked = 0m;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject && reader.CurrentDepth == currentDepth)
            {
                var result = new AccountUpdateEventBalance(asset, free, locked);

                return result;
            }

            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                var propertyName = reader.GetString();

                reader.Read();

                switch (propertyName)
                {
                    case "a":
                        asset = reader.GetString() ?? string.Empty;
                        break;
                    case "f":
                        free = reader.GetDecimalFromString();
                        break;
                    case "l":
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

    public override void Write(Utf8JsonWriter writer, AccountUpdateEventBalance value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
