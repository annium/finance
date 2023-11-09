using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Annium.Finance.Providers.Crypto.Binance.Base.Contracts.Shared.Domain;

namespace Annium.Finance.Providers.Crypto.Binance.Base.Contracts.Shared.Converters;

public class CommandResultConverter : JsonConverter<CommandResult?>
{
    public override CommandResult? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Read failed");
        }

        var currentDepth = reader.CurrentDepth;

        var id = long.MinValue;
        var hasResult = false;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject && reader.CurrentDepth == currentDepth)
            {
                return id != long.MinValue && hasResult ? new CommandResult(id) : null;
            }

            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                var propertyName = reader.GetString();

                reader.Read();

                switch (propertyName)
                {
                    case "id":
                        id = reader.GetInt64();
                        break;
                    case "result":
                        hasResult = true;
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }
        }

        throw new JsonException("Unexpected end of json");
    }

    public override void Write(Utf8JsonWriter writer, CommandResult? value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
