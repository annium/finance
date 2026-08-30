using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Annium.Finance.Providers.Crypto.Binance.Base.Shared.Contracts.Domain;

namespace Annium.Finance.Providers.Crypto.Binance.Base.Shared.Contracts.Converters;

/// <summary>
/// Converts a Binance WebSocket command acknowledgement (<c>{"id": ..., "result": ...}</c>, sent in reply to a
/// <c>SUBSCRIBE</c>/<c>UNSUBSCRIBE</c> request) into a <see cref="CommandResult"/>.
/// </summary>
public class CommandResultConverter : JsonConverter<CommandResult?>
{
    /// <summary>Reads a Binance command acknowledgement object into a <see cref="CommandResult"/>.</summary>
    /// <param name="reader">The reader positioned at the start of the acknowledgement object.</param>
    /// <param name="typeToConvert">The type being converted.</param>
    /// <param name="options">The active serializer options.</param>
    /// <returns>The parsed command result, or <c>null</c> if the <c>id</c> or <c>result</c> field was missing.</returns>
    public override CommandResult? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException($"Expected {JsonTokenType.StartObject}, got {reader.TokenType}");

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

    /// <summary>Not supported; command results are only ever read from Binance responses, never written.</summary>
    /// <param name="writer">The writer to write to.</param>
    /// <param name="value">The command result to write.</param>
    /// <param name="options">The active serializer options.</param>
    public override void Write(Utf8JsonWriter writer, CommandResult? value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
