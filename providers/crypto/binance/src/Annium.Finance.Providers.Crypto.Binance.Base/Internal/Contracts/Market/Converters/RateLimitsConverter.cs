using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Annium.Finance.Providers.Crypto.Binance.Base.Internal.Contracts.Market.Domain;

namespace Annium.Finance.Providers.Crypto.Binance.Base.Internal.Contracts.Market.Converters;

internal class RateLimitsConverter : JsonConverter<RateLimits?>
{
    public override RateLimits? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException("Read failed");
        }

        var currentDepth = reader.CurrentDepth;

        var items = new List<RateLimit>();

        var type = string.Empty;
        var limit = 0u;

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
                        limit = reader.GetUInt32();
                        break;

                    default:
                        reader.Skip();
                        break;
                }
            }
        }

        throw new JsonException("Unexpected end of json");
    }

    public override void Write(Utf8JsonWriter writer, RateLimits? value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}

file record RateLimit(string Type, uint Limit);
