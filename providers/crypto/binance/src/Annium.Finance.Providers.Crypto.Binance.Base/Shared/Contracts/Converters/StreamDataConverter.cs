using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Annium.Finance.Providers.Crypto.Binance.Base.Shared.Contracts.Domain;

namespace Annium.Finance.Providers.Crypto.Binance.Base.Shared.Contracts.Converters;

/// <summary>
/// Converts a Binance combined-stream envelope (<c>{"stream": ..., "data": ...}</c>) into a <see cref="StreamData{T}"/>.
/// </summary>
/// <typeparam name="T">The type of the payload carried in the <c>data</c> field.</typeparam>
public class StreamDataConverter<T> : JsonConverter<StreamData<T>?>
    where T : class
{
    /// <summary>Reads a Binance combined-stream envelope into a <see cref="StreamData{T}"/>, deserializing <c>data</c> as <typeparamref name="T"/>.</summary>
    /// <param name="reader">The reader positioned at the start of the envelope object.</param>
    /// <param name="typeToConvert">The type being converted.</param>
    /// <param name="options">The active serializer options.</param>
    /// <returns>The parsed stream envelope, or <c>null</c> if <c>stream</c> or <c>data</c> was missing.</returns>
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

    /// <summary>Not supported; stream envelopes are only ever read from Binance messages, never written.</summary>
    /// <param name="writer">The writer to write to.</param>
    /// <param name="value">The stream envelope to write.</param>
    /// <param name="options">The active serializer options.</param>
    public override void Write(Utf8JsonWriter writer, StreamData<T>? value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
