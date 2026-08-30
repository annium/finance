using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Annium.Finance.Providers.Crypto.Binance.Base.Market.Contracts.Domain;
using Annium.Serialization.Json;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Market.Contracts.Converters;

/// <summary>
/// Reads the <c>filters</c> array of a Binance exchange info symbol entry into an <see cref="InstrumentFilters"/>,
/// merging the separate <c>LOT_SIZE</c> and <c>MARKET_LOT_SIZE</c> filters into a single, more restrictive lot
/// size range. Writing is not supported since this contract is read-only (server-to-client).
/// </summary>
internal class InstrumentFiltersConverter : JsonConverter<InstrumentFilters>
{
    /// <summary>
    /// Reads the <c>PRICE_FILTER</c>, <c>LOT_SIZE</c>, <c>MARKET_LOT_SIZE</c>, <c>MIN_NOTIONAL</c> and
    /// <c>MAX_NUM_ORDERS</c> filter entries, combining the two lot size filters into their intersection.
    /// </summary>
    /// <param name="reader">The UTF-8 JSON reader positioned at the start of the filters array.</param>
    /// <param name="typeToConvert">The type being converted.</param>
    /// <param name="options">The serializer options in effect.</param>
    /// <returns>The combined filters, or null if any required filter is missing.</returns>
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
                    case "MIN_NOTIONAL":
                        notionalFilter = new NotionalFilter(minNotional, decimal.MaxValue);
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

                    case "notional":
                        minNotional = reader.GetDecimalFromString();
                        break;

                    case "limit":
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

    /// <summary>
    /// Not supported: instrument filters are only ever read from the exchange, never written.
    /// </summary>
    /// <param name="writer">The UTF-8 JSON writer.</param>
    /// <param name="value">The filters to write.</param>
    /// <param name="options">The serializer options in effect.</param>
    public override void Write(Utf8JsonWriter writer, InstrumentFilters value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
