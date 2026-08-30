using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Annium.Finance.Providers.Crypto.Binance.Base.Shared.Contracts.Domain;

namespace Annium.Finance.Providers.Crypto.Binance.Base.Shared.Contracts.Converters;

/// <summary>
/// Converts Binance's <c>/time</c> endpoint response (<c>{"serverTime": ...}</c>) into a <see cref="ServerTime"/>.
/// </summary>
public class ServerTimeConverter : JsonConverter<ServerTime?>
{
    /// <summary>Reads a Binance server time object into a <see cref="ServerTime"/>.</summary>
    /// <param name="reader">The reader positioned at the start of the server time object.</param>
    /// <param name="typeToConvert">The type being converted.</param>
    /// <param name="options">The active serializer options.</param>
    /// <returns>The parsed server time, or <c>null</c> if <c>serverTime</c> was missing or not positive.</returns>
    public override ServerTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException($"Expected {JsonTokenType.StartObject}, got {reader.TokenType}");

        var currentDepth = reader.CurrentDepth;

        var serverTime = 0L;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject && reader.CurrentDepth == currentDepth)
            {
                return serverTime > 0L ? new ServerTime(serverTime) : null;
            }

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
        }

        throw new JsonException("Unexpected end of json");
    }

    /// <summary>Not supported; server time is only ever read from Binance responses, never written.</summary>
    /// <param name="writer">The writer to write to.</param>
    /// <param name="value">The server time to write.</param>
    /// <param name="options">The active serializer options.</param>
    public override void Write(Utf8JsonWriter writer, ServerTime? value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
