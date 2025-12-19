using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Annium.Finance.Providers.Abstractions.Domain.Market;
using Annium.Serialization.Json;

namespace Annium.Finance.Providers.Crypto.Binance.Base.Contracts.Market.Converters;

public class CandleConverter : JsonConverter<CandleModel>
{
    public override CandleModel? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException($"Expected {JsonTokenType.StartArray}, got {reader.TokenType}");

        var currentDepth = reader.CurrentDepth;

        var timestamp = 0L;
        var open = 0m;
        var high = 0m;
        var low = 0m;
        var close = 0m;
        var volume = 0m;
        var index = 0;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray && reader.CurrentDepth == currentDepth)
            {
                if (timestamp == 0L)
                {
                    return default;
                }

                var candle = new CandleModel(timestamp, open, high, low, close, volume);

                return candle;
            }

            switch (index)
            {
                case 0:
                    timestamp = reader.GetInt64();
                    break;
                case 1:
                    open = reader.GetDecimalFromString();
                    break;
                case 2:
                    high = reader.GetDecimalFromString();
                    break;
                case 3:
                    low = reader.GetDecimalFromString();
                    break;
                case 4:
                    close = reader.GetDecimalFromString();
                    break;
                case 5:
                    volume = reader.GetDecimalFromString();
                    break;
                default:
                    reader.Skip();
                    break;
            }

            index++;
        }

        throw new JsonException("Unexpected end of json");
    }

    public override void Write(Utf8JsonWriter writer, CandleModel value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
