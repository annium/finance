using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Annium.Finance.Providers.Crypto.Binance.Base.Market.Contracts.Domain;
using Annium.Serialization.Json;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Internal.Market.Contracts.Converters;

/// <summary>
/// Deserializes a Binance symbol's <c>filters</c> array into an <see cref="InstrumentFilters"/>. Spot symbols
/// carry separate <c>LOT_SIZE</c> (limit order) and <c>MARKET_LOT_SIZE</c> (market order) filters, which are
/// merged into a single, most permissive-on-both-ends lot size filter.
/// </summary>
internal class InstrumentFiltersConverter : JsonConverter<InstrumentFilters>
{
    /// <summary>Reads a Binance symbol's <c>filters</c> array and converts it into an <see cref="InstrumentFilters"/>.</summary>
    /// <param name="reader">The reader positioned at the start of the filters array.</param>
    /// <param name="typeToConvert">The type being converted.</param>
    /// <param name="options">The active serializer options.</param>
    /// <returns>The converted filters, or null if any required filter is missing from the array.</returns>
    public override InstrumentFilters? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException($"Expected {JsonTokenType.StartArray}, got {reader.TokenType}");

        var currentDepth = reader.CurrentDepth;

        LotSizeFilter? limitLotSizeFilter = default;
        LotSizeFilter? marketLotSizeFilter = default;
        PriceFilter? priceFilter = default;
        NotionalFilter? notionalFilter = default;
        MaxOrdersFilter? maxOrdersFilter = default;

        var filterType = string.Empty;

        var minPrice = decimal.Zero;
        var maxPrice = decimal.Zero;
        var tickSize = decimal.Zero;

        var minQty = decimal.Zero;
        var maxQty = decimal.Zero;
        var stepSize = decimal.Zero;

        var minNotional = decimal.Zero;
        var maxNotional = decimal.Zero;

        var maxOrders = 0;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray && reader.CurrentDepth == currentDepth)
            {
                if (
                    priceFilter is null
                    || limitLotSizeFilter is null
                    || marketLotSizeFilter is null
                    || notionalFilter is null
                    || maxOrdersFilter is null
                )
                {
                    return null;
                }

                var lotSizeFilter = new LotSizeFilter(
                    Math.Max(limitLotSizeFilter.MinQty, marketLotSizeFilter.MinQty),
                    Math.Min(limitLotSizeFilter.MaxQty, marketLotSizeFilter.MaxQty),
                    Math.Max(limitLotSizeFilter.StepSize, marketLotSizeFilter.StepSize)
                );
                var result = new InstrumentFilters(lotSizeFilter, priceFilter, notionalFilter, maxOrdersFilter);

                return result;
            }

            if (reader.TokenType == JsonTokenType.StartObject)
            {
                filterType = string.Empty;
            }

            if (reader.TokenType == JsonTokenType.EndObject)
            {
                switch (filterType)
                {
                    case "PRICE_FILTER":
                        priceFilter = new PriceFilter(minPrice, maxPrice, tickSize);
                        break;
                    case "LOT_SIZE":
                        limitLotSizeFilter = new LotSizeFilter(minQty, maxQty, stepSize);
                        break;
                    case "MARKET_LOT_SIZE":
                        marketLotSizeFilter = new LotSizeFilter(minQty, maxQty, stepSize);
                        break;
                    case "NOTIONAL":
                        notionalFilter = new NotionalFilter(minNotional, maxNotional);
                        break;
                    case "MAX_NUM_ORDERS":
                        maxOrdersFilter = new MaxOrdersFilter(maxOrders);
                        break;
                }
            }

            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                var propertyName = reader.GetString();

                reader.Read();

                switch (propertyName)
                {
                    case "filterType":
                        filterType = reader.GetString();
                        break;

                    case "minPrice":
                        minPrice = reader.GetDecimalFromString();
                        break;
                    case "maxPrice":
                        maxPrice = reader.GetDecimalFromString();
                        break;
                    case "tickSize":
                        tickSize = reader.GetDecimalFromString();
                        break;

                    case "minQty":
                        minQty = reader.GetDecimalFromString();
                        break;
                    case "maxQty":
                        maxQty = reader.GetDecimalFromString();
                        break;
                    case "stepSize":
                        stepSize = reader.GetDecimalFromString();
                        break;

                    case "minNotional":
                        minNotional = reader.GetDecimalFromString();
                        break;
                    case "maxNotional":
                        maxNotional = reader.GetDecimalFromString();
                        break;

                    case "maxNumOrders":
                        maxOrders = reader.GetInt32();
                        break;

                    default:
                        reader.Skip();
                        break;
                }
            }
        }

        throw new JsonException("Unexpected end of json");
    }

    /// <summary>Not supported; instrument filters are only ever read from Binance, never written.</summary>
    /// <param name="writer">The writer to serialize to.</param>
    /// <param name="value">The value to serialize.</param>
    /// <param name="options">The active serializer options.</param>
    public override void Write(Utf8JsonWriter writer, InstrumentFilters value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
