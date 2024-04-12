using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Annium.Finance.Providers.Crypto.Binance.Base.Contracts.Shared.Domain;

namespace Annium.Finance.Providers.Crypto.Binance.Base.Contracts.Shared.Converters;

public class StreamDataConverter<T> : JsonConverter<StreamData<T>?>
    where T : class
{
    public override StreamData<T>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException($"Expected {JsonTokenType.StartObject}, got {reader.TokenType}");

        var currentDepth = reader.CurrentDepth;

        var stream = string.Empty;
        var data = default(T);

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject && reader.CurrentDepth == currentDepth)
            {
                return stream is not null && data is not null ? new StreamData<T>(stream, data) : null;
            }

            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                var propertyName = reader.GetString();

                reader.Read();

                switch (propertyName)
                {
                    case "stream":
                        stream = reader.GetString();
                        break;
                    case "data":
                        data = JsonSerializer.Deserialize<T>(ref reader, options);
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }
        }

        throw new JsonException("Unexpected end of json");
    }

    public override void Write(Utf8JsonWriter writer, StreamData<T>? value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
