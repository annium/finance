using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Annium.Finance.Providers.Abstractions.Domain.Market;
using Annium.Finance.Providers.Crypto.Binance.Base.Market.Contracts.Domain;
using Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Market.Contracts.Domain;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Market.Contracts.Converters;

/// <summary>
/// Reads the <c>GET /fapi/v1/exchangeInfo</c> response into an <see cref="ExchangeInfo"/>, delegating rate
/// limits, assets and instruments to their own converters. Writing is not supported since this contract is
/// read-only (server-to-client).
/// </summary>
internal class ExchangeInfoConverter : JsonConverter<ExchangeInfo?>
{
    /// <summary>
    /// Reads the rate limits, assets and instruments (symbols) reported by the exchange. Instruments that fail
    /// to parse are silently dropped rather than failing the whole response.
    /// </summary>
    /// <param name="reader">The UTF-8 JSON reader positioned at the start of the exchange info object.</param>
    /// <param name="typeToConvert">The type being converted.</param>
    /// <param name="options">The serializer options in effect.</param>
    /// <returns>The parsed exchange info, or null if any required section is missing.</returns>
    public override ExchangeInfo? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException($"Expected {JsonTokenType.StartObject}, got {reader.TokenType}");

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

    /// <summary>
    /// Not supported: exchange info is only ever read from the exchange, never written.
    /// </summary>
    /// <param name="writer">The UTF-8 JSON writer.</param>
    /// <param name="value">The exchange info to write.</param>
    /// <param name="options">The serializer options in effect.</param>
    public override void Write(Utf8JsonWriter writer, ExchangeInfo? value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
