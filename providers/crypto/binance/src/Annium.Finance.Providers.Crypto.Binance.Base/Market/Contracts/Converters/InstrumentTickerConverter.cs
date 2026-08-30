using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Annium.Finance.Providers.Abstractions.Domain.Market;
using Annium.Serialization.Json;

namespace Annium.Finance.Providers.Crypto.Binance.Base.Market.Contracts.Converters;

/// <summary>
/// Converts a Binance book ticker payload (<c>s</c>/<c>b</c>/<c>a</c> for symbol/bid price/ask price) into an <see cref="InstrumentTicker"/>.
/// </summary>
public class InstrumentTickerConverter : JsonConverter<InstrumentTicker>
{
    /// <summary>Reads a Binance book ticker object into an <see cref="InstrumentTicker"/>.</summary>
    /// <param name="reader">The reader positioned at the start of the ticker object.</param>
    /// <param name="typeToConvert">The type being converted.</param>
    /// <param name="options">The active serializer options.</param>
    /// <returns>The parsed ticker, or <c>null</c> if the symbol was missing or both prices were zero.</returns>
    public override InstrumentTicker? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException($"Expected {JsonTokenType.StartObject}, got {reader.TokenType}");

        var currentDepth = reader.CurrentDepth;

        var symbol = string.Empty;
        var askPrice = 0m;
        var bidPrice = 0m;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject && reader.CurrentDepth == currentDepth)
            {
                if (string.IsNullOrWhiteSpace(symbol) || (bidPrice == 0 && askPrice == 0))
                {
                    return null;
                }

                var result = new InstrumentTicker(symbol, bidPrice, askPrice);

                return result;
            }

            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                var propertyName = reader.GetString();

                reader.Read();

                switch (propertyName)
                {
                    case "s":
                        symbol = reader.GetString();
                        break;
                    case "a":
                        askPrice = reader.GetDecimalFromString();
                        break;
                    case "b":
                        bidPrice = reader.GetDecimalFromString();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }
        }

        throw new JsonException("Unexpected end of json");
    }

    /// <summary>Not supported; tickers are only ever read from Binance responses, never written.</summary>
    /// <param name="writer">The writer to write to.</param>
    /// <param name="value">The ticker to write.</param>
    /// <param name="options">The active serializer options.</param>
    public override void Write(Utf8JsonWriter writer, InstrumentTicker value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
