using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Annium.Finance.Providers.Abstractions.Domain.Market;
using Annium.Finance.Providers.Crypto.Binance.Base.Market.Contracts.Domain;
using Annium.Finance.Providers.Crypto.Binance.Spot.Internal.Market.Contracts.Domain;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Internal.Market.Contracts.Converters;

/// <summary>
/// Deserializes the Binance spot exchange info response into an <see cref="ExchangeInfo"/>, combining the rate
/// limits with the tradable instruments (<c>symbols</c> that survive <see cref="InstrumentConverter"/> filtering).
/// </summary>
internal class ExchangeInfoConverter : JsonConverter<ExchangeInfo?>
{
    /// <summary>Reads a Binance exchange info response and converts it into an <see cref="ExchangeInfo"/>.</summary>
    /// <param name="reader">The reader positioned at the start of the response object.</param>
    /// <param name="typeToConvert">The type being converted.</param>
    /// <param name="options">The active serializer options.</param>
    /// <returns>The converted exchange info, or null if the rate limits or the instrument list are missing.</returns>
    public override ExchangeInfo? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException($"Expected {JsonTokenType.StartObject}, got {reader.TokenType}");

        var currentDepth = reader.CurrentDepth;

        var rateLimits = default(RateLimits?);
        var instruments = default(IReadOnlyCollection<InstrumentModel>);

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject && reader.CurrentDepth == currentDepth)
            {
                if (rateLimits is null || instruments is null)
                {
                    return default;
                }

                return new ExchangeInfo(rateLimits, instruments);
            }

            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                var propertyName = reader.GetString();

                reader.Read();

                switch (propertyName)
                {
                    case "rateLimits":
                        rateLimits = JsonSerializer.Deserialize<RateLimits?>(ref reader, options);
                        break;
                    case "symbols":
                        var allInstruments = JsonSerializer.Deserialize<IReadOnlyCollection<InstrumentModel?>>(
                            ref reader,
                            options
                        );
                        instruments = allInstruments?.OfType<InstrumentModel>().ToArray();
                        break;

                    default:
                        reader.Skip();
                        break;
                }
            }
        }

        throw new JsonException("Unexpected end of json");
    }

    /// <summary>Not supported; exchange info is only ever read from Binance, never written.</summary>
    /// <param name="writer">The writer to serialize to.</param>
    /// <param name="value">The value to serialize.</param>
    /// <param name="options">The active serializer options.</param>
    public override void Write(Utf8JsonWriter writer, ExchangeInfo? value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
