using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Annium.Finance.Providers.Crypto.Binance.Base.User.Contracts.Domain;

namespace Annium.Finance.Providers.Crypto.Binance.Base.User.Contracts.Converters;

/// <summary>
/// Converts Binance's listen key endpoint response (<c>{"listenKey": ...}</c>) into a <see cref="ListenKey"/>.
/// </summary>
public class ListenKeyResponseConverter : JsonConverter<ListenKey>
{
    /// <summary>Reads a Binance listen key response object into a <see cref="ListenKey"/>.</summary>
    /// <param name="reader">The reader positioned at the start of the response object.</param>
    /// <param name="typeToConvert">The type being converted.</param>
    /// <param name="options">The active serializer options.</param>
    /// <returns>The parsed listen key, or <c>null</c> if <c>listenKey</c> was missing.</returns>
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

    /// <summary>Not supported; listen keys are only ever read from Binance responses, never written.</summary>
    /// <param name="writer">The writer to write to.</param>
    /// <param name="value">The listen key to write.</param>
    /// <param name="options">The active serializer options.</param>
    public override void Write(Utf8JsonWriter writer, ListenKey value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
