using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Annium.Finance.Providers.Crypto.Binance.Base.Internal.Contracts.Market.Domain;
using Annium.Serialization.Json;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Contracts.Market.Converters;

internal class InstrumentFiltersConverter : JsonConverter<InstrumentFilters>
{
    public override InstrumentFilters? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException("Read failed");
        }

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

    public override void Write(Utf8JsonWriter writer, InstrumentFilters value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
