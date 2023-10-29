using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Annium.Finance.Providers.Abstractions.Domain.Models;
using Annium.Serialization.Json;

namespace Annium.Finance.Providers.Crypto.Binance.Base.Internal.Contracts.Market.Converters;

internal class InstrumentTickerConverter : JsonConverter<InstrumentTicker>
{
    public override InstrumentTicker? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Read failed");
        }

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

    public override void Write(Utf8JsonWriter writer, InstrumentTicker value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
