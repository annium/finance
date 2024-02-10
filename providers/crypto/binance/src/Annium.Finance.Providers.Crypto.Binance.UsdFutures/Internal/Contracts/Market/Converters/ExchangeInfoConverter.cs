using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Annium.Finance.Providers.Abstractions.Domain.Models;
using Annium.Finance.Providers.Crypto.Binance.Base.Contracts.Market.Domain;
using Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Contracts.Market.Domain;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Contracts.Market.Converters;

internal class ExchangeInfoConverter : JsonConverter<ExchangeInfo?>
{
    public override ExchangeInfo? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Read failed");
        }

        var currentDepth = reader.CurrentDepth;

        var rateLimits = default(RateLimits?);
        var assets = default(IReadOnlyCollection<Asset>);
        var instruments = default(IReadOnlyCollection<InstrumentModel>);

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject && reader.CurrentDepth == currentDepth)
            {
                if (rateLimits is null || assets is null || instruments is null)
                {
                    return default;
                }

                return new ExchangeInfo(rateLimits, assets, instruments);
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
                    case "assets":
                        assets = JsonSerializer.Deserialize<IReadOnlyCollection<Asset>?>(ref reader, options);
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

    public override void Write(Utf8JsonWriter writer, ExchangeInfo? value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
