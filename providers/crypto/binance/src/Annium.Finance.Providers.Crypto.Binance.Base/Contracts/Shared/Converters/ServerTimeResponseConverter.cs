using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Annium.Finance.Providers.Crypto.Binance.Base.Contracts.Shared.Domain;

namespace Annium.Finance.Providers.Crypto.Binance.Base.Contracts.Shared.Converters;

public class ServerTimeResponseConverter : JsonConverter<ServerTime>
{
    public override ServerTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Read failed");
        }

        var currentDepth = reader.CurrentDepth;

        var serverTime = 0L;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                var propertyName = reader.GetString();

                reader.Read();

                switch (propertyName)
                {
                    case "serverTime":
                        serverTime = reader.GetInt64();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }
            else if (reader.TokenType == JsonTokenType.EndObject && reader.CurrentDepth == currentDepth)
            {
                return new ServerTime(serverTime);
            }
        }

        throw new JsonException("Unexpected end of json");
    }

    public override void Write(Utf8JsonWriter writer, ServerTime value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
