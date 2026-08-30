using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Annium.Finance.Providers.Abstractions.Domain.Market;
using Annium.Serialization.Json;

namespace Annium.Finance.Providers.Crypto.Binance.Base.Market.Contracts.Converters;

/// <summary>
/// Converts a Binance kline/candlestick array (<c>[openTime, open, high, low, close, volume, ...]</c>) into a <see cref="CandleModel"/>.
/// </summary>
public class CandleConverter : JsonConverter<CandleModel>
{
    /// <summary>Reads a Binance kline array into a <see cref="CandleModel"/>, ignoring any trailing fields beyond volume.</summary>
    /// <param name="reader">The reader positioned at the start of the kline array.</param>
    /// <param name="typeToConvert">The type being converted.</param>
    /// <param name="options">The active serializer options.</param>
    /// <returns>The parsed candle, or <c>null</c> if the array had no open time.</returns>
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

    /// <summary>Not supported; candles are only ever read from Binance responses, never written.</summary>
    /// <param name="writer">The writer to write to.</param>
    /// <param name="value">The candle to write.</param>
    /// <param name="options">The active serializer options.</param>
    public override void Write(Utf8JsonWriter writer, CandleModel value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
