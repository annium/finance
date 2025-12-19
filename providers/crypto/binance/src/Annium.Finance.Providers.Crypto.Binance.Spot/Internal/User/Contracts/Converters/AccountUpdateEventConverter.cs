using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Annium.Finance.Providers.Crypto.Binance.Spot.Internal.User.Contracts.Domain;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Internal.User.Contracts.Converters;

internal class AccountUpdateEventConverter : JsonConverter<AccountUpdateEvent?>
{
    public override AccountUpdateEvent? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException($"Expected {JsonTokenType.StartObject}, got {reader.TokenType}");

        var currentDepth = reader.CurrentDepth;

        var canConvert = true;
        var date = 0L;
        IReadOnlyCollection<AccountUpdateEventBalance>? balances = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject && reader.CurrentDepth == currentDepth)
            {
                if (!canConvert || balances is null)
                {
                    return default;
                }

                var result = new AccountUpdateEvent(date, balances);

                return result;
            }

            if (!canConvert)
            {
                reader.Skip();
                continue;
            }

            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                var propertyName = reader.GetString();

                reader.Read();

                switch (propertyName)
                {
                    case "e":
                        var eventType = reader.GetString();
                        if (eventType != "outboundAccountPosition")
                        {
                            canConvert = false;
                        }

                        break;
                    case "u":
                        date = reader.GetInt64();
                        break;
                    case "B":
                        balances = JsonSerializer.Deserialize<IReadOnlyCollection<AccountUpdateEventBalance>>(
                            ref reader,
                            options
                        );
                        break;

                    default:
                        reader.Skip();
                        break;
                }
            }
        }

        throw new JsonException("Unexpected end of json");
    }

    public override void Write(Utf8JsonWriter writer, AccountUpdateEvent? value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
