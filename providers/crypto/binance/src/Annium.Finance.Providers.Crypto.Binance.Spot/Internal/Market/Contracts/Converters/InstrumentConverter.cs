using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Annium.Finance.Providers.Abstractions.Domain.Market;
using Annium.Finance.Providers.Crypto.Binance.Base.Market.Contracts.Domain;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Internal.Market.Contracts.Converters;

/// <summary>
/// Deserializes a Binance exchange info <c>symbols</c> entry into an <see cref="InstrumentModel"/>, keeping only
/// symbols that are actively tradable on spot.
/// </summary>
internal class InstrumentConverter : JsonConverter<InstrumentModel>
{
    /// <summary>The Binance <c>status</c> value a symbol must have to be considered tradable.</summary>
    private const string RequiredStatus = "TRADING";

    /// <summary>The Binance trading permission a symbol must carry, either directly or via a permission set, to be considered a spot instrument.</summary>
    private const string RequiredPermission = "SPOT";

    /// <summary>Reads a Binance <c>symbols</c> entry and converts it into an <see cref="InstrumentModel"/>.</summary>
    /// <param name="reader">The reader positioned at the start of the symbol object.</param>
    /// <param name="typeToConvert">The type being converted.</param>
    /// <param name="options">The active serializer options.</param>
    /// <returns>The converted instrument, or null if the symbol is not tradable on spot or is missing required fields.</returns>
    public override InstrumentModel? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException($"Expected {JsonTokenType.StartObject}, got {reader.TokenType}");

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
        var permissionSets = new HashSet<string>();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject && reader.CurrentDepth == currentDepth)
            {
                if (
                    status != RequiredStatus
                    || !isSpotTradingAllowed
                    || filters is null
                    || !(permissions.Contains(RequiredPermission) || permissionSets.Contains(RequiredPermission))
                )
                {
                    return default;
                }

                var target = new ResourceModel(baseAsset, baseAssetPrecision);
                var quote = new ResourceModel(quoteAsset, quoteAssetPrecision);

                var instrument = new InstrumentModel(
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
                        permissions = [.. JsonSerializer.Deserialize<string[]>(ref reader, options) ?? []];
                        break;
                    case "permissionSets":
                        permissionSets = (JsonSerializer.Deserialize<string[][]>(ref reader, options) ?? [])
                            .SelectMany(x => x)
                            .ToHashSet();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }
        }

        throw new JsonException("Unexpected end of json");
    }

    /// <summary>Not supported; instruments are only ever read from Binance, never written.</summary>
    /// <param name="writer">The writer to serialize to.</param>
    /// <param name="value">The value to serialize.</param>
    /// <param name="options">The active serializer options.</param>
    public override void Write(Utf8JsonWriter writer, InstrumentModel value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
