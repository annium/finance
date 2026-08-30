using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Annium.Finance.Providers.Abstractions.Domain.Market;
using Annium.Finance.Providers.Crypto.Binance.Base.Market.Contracts.Domain;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Market.Contracts.Converters;

/// <summary>
/// Reads a Binance exchange info symbol entry into an <see cref="InstrumentModel"/>, keeping only tradable
/// perpetual contracts. Writing is not supported since this contract is read-only (server-to-client).
/// </summary>
internal class InstrumentConverter : JsonConverter<InstrumentModel>
{
    /// <summary>The <c>contractType</c> value that identifies a perpetual (as opposed to a delivery) contract.</summary>
    private const string RequiredContractType = "PERPETUAL";

    /// <summary>The <c>status</c> value that identifies a symbol currently open for trading.</summary>
    private const string RequiredStatus = "TRADING";

    /// <summary>
    /// Reads a symbol entry, discarding it unless it is a currently-trading perpetual contract with all its
    /// filters present.
    /// </summary>
    /// <param name="reader">The UTF-8 JSON reader positioned at the start of the symbol object.</param>
    /// <param name="typeToConvert">The type being converted.</param>
    /// <param name="options">The serializer options in effect.</param>
    /// <returns>The parsed instrument, or null if it is not a tradable perpetual contract.</returns>
    public override InstrumentModel? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException($"Expected {JsonTokenType.StartObject}, got {reader.TokenType}");

        var currentDepth = reader.CurrentDepth;

        var symbol = string.Empty;
        var contractType = string.Empty;
        var status = string.Empty;
        var baseAsset = string.Empty;
        byte baseAssetPrecision = 0;
        var quoteAsset = string.Empty;
        byte quoteAssetPrecision = 0;
        var filters = default(InstrumentFilters);

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject && reader.CurrentDepth == currentDepth)
            {
                if (contractType != RequiredContractType || status != RequiredStatus || filters is null)
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
                    case "contractType":
                        contractType = reader.GetString().NotNull();
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
                    case "quotePrecision":
                        quoteAssetPrecision = reader.GetByte();
                        break;
                    case "filters":
                        filters = JsonSerializer.Deserialize<InstrumentFilters>(ref reader, options);
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
    /// Not supported: instruments are only ever read from the exchange, never written.
    /// </summary>
    /// <param name="writer">The UTF-8 JSON writer.</param>
    /// <param name="value">The instrument to write.</param>
    /// <param name="options">The serializer options in effect.</param>
    public override void Write(Utf8JsonWriter writer, InstrumentModel value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
