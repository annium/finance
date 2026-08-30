using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Annium.Finance.Providers.Crypto.Binance.Base.Market.Contracts.Domain;

namespace Annium.Finance.Providers.Crypto.Binance.Base.Market.Contracts.Converters;

/// <summary>
/// Converts the <c>rateLimits</c> array of Binance's <c>exchangeInfo</c> response into <see cref="RateLimits"/>,
/// picking out the <c>REQUEST_WEIGHT</c> entry as the request weight limit.
/// </summary>
public class RateLimitsConverter : JsonConverter<RateLimits?>
{
    /// <summary>Reads the <c>rateLimits</c> array and extracts the <c>REQUEST_WEIGHT</c> limit into a <see cref="RateLimits"/>.</summary>
    /// <param name="reader">The reader positioned at the start of the rate limits array.</param>
    /// <param name="typeToConvert">The type being converted.</param>
    /// <param name="options">The active serializer options.</param>
    /// <returns>The parsed rate limits, or <c>null</c> if no <c>REQUEST_WEIGHT</c> entry was present.</returns>
    public override RateLimits? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException($"Expected {JsonTokenType.StartArray}, got {reader.TokenType}");

        var currentDepth = reader.CurrentDepth;

        var items = new List<RateLimit>();

        var type = string.Empty;
        var limit = 0;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray && reader.CurrentDepth == currentDepth)
            {
                var requestLimit = items.FirstOrDefault(item => item.Type == "REQUEST_WEIGHT");
                if (requestLimit is null)
                {
                    return default;
                }

                // as of now, it's assumed, that request limit is already specified for 1 minute interval to simplify this converter
                var result = new RateLimits(requestLimit.Limit);

                return result;
            }

            if (reader.TokenType == JsonTokenType.StartObject)
            {
                type = string.Empty;
                limit = default;
            }

            if (reader.TokenType == JsonTokenType.EndObject && !string.IsNullOrWhiteSpace(type))
            {
                items.Add(new RateLimit(type, limit));
            }

            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                var propertyName = reader.GetString();

                reader.Read();

                switch (propertyName)
                {
                    case "rateLimitType":
                        type = reader.GetString();
                        break;
                    case "limit":
                        limit = reader.GetInt32();
                        break;

                    default:
                        reader.Skip();
                        break;
                }
            }
        }

        throw new JsonException("Unexpected end of json");
    }

    /// <summary>Not supported; rate limits are only ever read from Binance responses, never written.</summary>
    /// <param name="writer">The writer to write to.</param>
    /// <param name="value">The rate limits to write.</param>
    /// <param name="options">The active serializer options.</param>
    public override void Write(Utf8JsonWriter writer, RateLimits? value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}

/// <summary>A single entry from Binance's <c>rateLimits</c> array, before it is filtered down to the <c>REQUEST_WEIGHT</c> entry.</summary>
/// <param name="Type">The rate limit type, e.g. <c>REQUEST_WEIGHT</c> or <c>ORDERS</c>.</param>
/// <param name="Limit">The maximum value allowed for this rate limit type within its interval.</param>
file record RateLimit(string Type, int Limit);
