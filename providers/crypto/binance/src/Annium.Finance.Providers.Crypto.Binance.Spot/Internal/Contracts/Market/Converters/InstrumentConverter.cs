using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Annium.Finance.Providers.Abstractions.Domain.Dto;
using Annium.Finance.Providers.Crypto.Binance.Base.Contracts.Market.Domain;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Internal.Contracts.Market.Converters;

internal class InstrumentConverter : JsonConverter<InstrumentDto>
{
    private const string RequiredStatus = "TRADING";
    private const string RequiredPermission = "SPOT";

    public override InstrumentDto? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Read failed");
        }

        var currentDepth = reader.CurrentDepth;

        var symbol = string.Empty;
        var status = string.Empty;
        var baseAsset = string.Empty;
        byte baseAssetPrecision = 0;
        var quoteAsset = string.Empty;
        byte quoteAssetPrecision = 0;
        var isSpotTradingAllowed = false;
        var filters = default(InstrumentFilters);
        var permissions = new HashSet<string>();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject && reader.CurrentDepth == currentDepth)
            {
                if (
                    status != RequiredStatus
                    || !isSpotTradingAllowed
                    || filters is null
                    || !permissions.Contains(RequiredPermission)
                )
                {
                    return default;
                }

                var target = new ResourceDto(baseAsset, baseAssetPrecision);
                var quote = new ResourceDto(quoteAsset, quoteAssetPrecision);

                var instrument = new InstrumentDto(
                    symbol,
                    target,
                    quote,
                    quote,
                    filters.LotSizeFilter.MinQty,
                    filters.LotSizeFilter.MaxQty,
                    filters.LotSizeFilter.StepSize,
                    filters.PriceFilter.MinPrice,
                    filters.PriceFilter.MaxPrice,
                    filters.PriceFilter.TickSize,
                    filters.NotionalFilter.MinNotional,
                    filters.NotionalFilter.MaxNotional,
                    filters.MaxOrdersFilter.MaxOrders
                );

                return instrument;
            }

            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                var propertyName = reader.GetString();

                reader.Read();

                switch (propertyName)
                {
                    case "symbol":
                        symbol = reader.GetString().NotNull();
                        break;
                    case "status":
                        status = reader.GetString().NotNull();
                        break;
                    case "baseAsset":
                        baseAsset = reader.GetString().NotNull();
                        break;
                    case "baseAssetPrecision":
                        baseAssetPrecision = reader.GetByte();
                        break;
                    case "quoteAsset":
                        quoteAsset = reader.GetString().NotNull();
                        break;
                    case "quoteAssetPrecision":
                        quoteAssetPrecision = reader.GetByte();
                        break;
                    case "isSpotTradingAllowed":
                        isSpotTradingAllowed = reader.GetBoolean();
                        break;
                    case "filters":
                        filters = JsonSerializer.Deserialize<InstrumentFilters>(ref reader, options);
                        break;
                    case "permissions":
                        permissions = new HashSet<string>(
                            JsonSerializer.Deserialize<string[]>(ref reader, options) ?? Array.Empty<string>()
                        );
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }
        }

        throw new JsonException("Unexpected end of json");
    }

    public override void Write(Utf8JsonWriter writer, InstrumentDto value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
