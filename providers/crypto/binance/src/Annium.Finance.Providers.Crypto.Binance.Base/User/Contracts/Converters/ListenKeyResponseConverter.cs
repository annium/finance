using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Annium.Finance.Providers.Crypto.Binance.Base.User.Contracts.Domain;

namespace Annium.Finance.Providers.Crypto.Binance.Base.User.Contracts.Converters;

public class ListenKeyResponseConverter : JsonConverter<ListenKey>
{
    public override ListenKey? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException($"Expected {JsonTokenType.StartObject}, got {reader.TokenType}");

        var currentDepth = reader.CurrentDepth;

        string? listenKey = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                var propertyName = reader.GetString();

                reader.Read();

                switch (propertyName)
                {
                    case "listenKey":
                        listenKey = reader.GetString();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }
            else if (reader.TokenType == JsonTokenType.EndObject && reader.CurrentDepth == currentDepth)
            {
                return listenKey is not null ? new ListenKey(listenKey) : default;
            }
        }

        throw new JsonException("Unexpected end of json");
    }

    public override void Write(Utf8JsonWriter writer, ListenKey value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
