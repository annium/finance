using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Annium.Finance.Providers.Abstractions.Domain.User;
using Annium.Finance.Providers.Crypto.Binance.Spot.Internal.User.Contracts.Domain;
using Annium.Linq;
using Annium.Serialization.Json;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Internal.User.Contracts.Converters;

/// <summary>
/// Deserializes a Binance user data stream event into an <see cref="OrderUpdateEvent"/>, matching only events
/// whose <c>e</c> field is <c>executionReport</c>.
/// </summary>
internal class OrderUpdateEventConverter : JsonConverter<OrderUpdateEvent?>
{
    /// <summary>Reads a Binance user data stream event and converts it into an <see cref="OrderUpdateEvent"/>.</summary>
    /// <param name="reader">The reader positioned at the start of the event object.</param>
    /// <param name="typeToConvert">The type being converted.</param>
    /// <param name="options">The active serializer options.</param>
    /// <returns>The converted event, or null if the event is not an <c>executionReport</c> event or is missing the order id.</returns>
    public override OrderUpdateEvent? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException($"Expected {JsonTokenType.StartObject}, got {reader.TokenType}");

        var currentDepth = reader.CurrentDepth;

        var canConvert = true;
        var symbol = string.Empty;
        var tradeId = string.Empty;
        var orderId = 0L;
        var clientOrderId = string.Empty;
        var type = default(OrderType);
        var side = default(OrderSide);
        var totalQty = 0m;
        var price = 0m;
        var levelPrice = 0m;
        var status = default(OrderStatus);
        var executedQty = 0m;
        var executedSum = 0m;
        var lastExecutedQty = 0m;
        var lastExecutedPrice = 0m;
        var commissionAmount = 0m;
        var commissionAsset = string.Empty;
        var isMaker = false;
        var createdAt = 0L;
        var updatedAt = 0L;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject && reader.CurrentDepth == currentDepth)
            {
                if (!canConvert || orderId == 0L)
                {
                    return default;
                }

                var result = new OrderUpdateEvent(
                    symbol ?? string.Empty,
                    tradeId,
                    orderId.ToString(),
                    clientOrderId ?? string.Empty,
                    OrientationRange.Both,
                    type,
                    side,
                    totalQty,
                    price,
                    levelPrice,
                    status,
                    executedQty,
                    executedQty == 0 ? 0 : executedSum / executedQty,
                    lastExecutedQty,
                    lastExecutedPrice,
                    commissionAmount,
                    commissionAsset ?? string.Empty,
                    isMaker,
                    createdAt,
                    updatedAt
                );

                return result;
            }

            if (!canConvert)
            {
                reader.Skip();
                continue;
            }

            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                var propertyName = reader.GetString();

                reader.Read();

                switch (propertyName)
                {
                    case "e":
                        var eventType = reader.GetString();
                        if (eventType != "executionReport")
                        {
                            canConvert = false;
                        }

                        break;
                    case "s":
                        symbol = reader.GetString();
                        break;
                    case "t":
                        tradeId = reader.GetInt64().ToString();
                        break;
                    case "i":
                        orderId = reader.GetInt64();
                        break;
                    case "c":
                        clientOrderId = reader.GetString();
                        break;
                    case "o":
                        type = OrderTypes.StringToValue.MapValue(reader.GetString());
                        break;
                    case "S":
                        side = OrderSides.StringToValue.MapValue(reader.GetString());
                        break;
                    case "q":
                        totalQty = reader.GetDecimalFromString();
                        break;
                    case "p":
                        price = reader.GetDecimalFromString();
                        break;
                    case "P":
                        levelPrice = reader.GetDecimalFromString();
                        break;
                    case "X":
                        status = OrderStatuses.StringToValue.MapValue(reader.GetString());
                        break;
                    case "z":
                        executedQty = reader.GetDecimalFromString();
                        break;
                    case "Z":
                        executedSum = reader.GetDecimalFromString();
                        break;
                    case "l":
                        lastExecutedQty = reader.GetDecimalFromString();
                        break;
                    case "L":
                        lastExecutedPrice = reader.GetDecimalFromString();
                        break;
                    case "n":
                        commissionAmount = reader.GetDecimalFromString();
                        break;
                    case "N":
                        commissionAsset = reader.GetString();
                        break;
                    case "m":
                        isMaker = reader.GetBoolean();
                        break;
                    case "O":
                        createdAt = reader.GetInt64();
                        break;
                    case "T":
                        updatedAt = reader.GetInt64();
                        break;

                    default:
                        reader.Skip();
                        break;
                }
            }
        }

        throw new JsonException("Unexpected end of json");
    }

    /// <summary>Not supported; order update events are only ever read from the Binance user data stream.</summary>
    /// <param name="writer">The writer to serialize to.</param>
    /// <param name="value">The value to serialize.</param>
    /// <param name="options">The active serializer options.</param>
    public override void Write(Utf8JsonWriter writer, OrderUpdateEvent? value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
