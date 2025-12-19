using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Annium.Finance.Providers.Crypto.Binance.Base.Shared.Contracts.Domain;

namespace Annium.Finance.Providers.Crypto.Binance.Base.Shared.Contracts.Converters;

public class OperationResultConverter : JsonConverter<OperationResult?>
{
    public override OperationResult? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException($"Expected {JsonTokenType.StartObject}, got {reader.TokenType}");

        var currentDepth = reader.CurrentDepth;

        var code = long.MinValue;
        string? message = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject && reader.CurrentDepth == currentDepth)
            {
                return code != long.MinValue && message is not null ? new OperationResult(code, message) : null;
            }

            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                var propertyName = reader.GetString();

                reader.Read();

                switch (propertyName)
                {
                    case "code":
                        code = reader.GetInt64();
                        break;
                    case "msg":
                        message = reader.GetString() ?? string.Empty;
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }
        }

        throw new JsonException("Unexpected end of json");
    }

    public override void Write(Utf8JsonWriter writer, OperationResult? value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
