using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Annium.Finance.Providers.Abstractions.Domain.User;
using Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.User.Contracts.Domain;
using Annium.Linq;
using Annium.Serialization.Json;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.User.Contracts.Converters;

/// <summary>
/// Reads a user data stream <c>ORDER_TRADE_UPDATE</c> event into an <see cref="OrderUpdateEvent"/>, unwrapping
/// the nested <c>o</c> order object. Writing is not supported since this contract is read-only
/// (server-to-client).
/// </summary>
internal class OrderUpdateEventConverter : JsonConverter<OrderUpdateEvent?>
{
    /// <summary>
    /// Reads the event, rejecting the payload unless its <c>e</c> field is <c>ORDER_TRADE_UPDATE</c>.
    /// </summary>
    /// <param name="reader">The UTF-8 JSON reader positioned at the start of the event object.</param>
    /// <param name="typeToConvert">The type being converted.</param>
    /// <param name="options">The serializer options in effect.</param>
    /// <returns>The parsed event, or null if the payload is not an order update event or has no order id.</returns>
    public override OrderUpdateEvent? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException($"Expected {JsonTokenType.StartObject}, got {reader.TokenType}");

        var currentDepth = reader.CurrentDepth;

        var canConvert = true;
        var tradeId = string.Empty;
        var orderId = 0L;
        var clientOrderId = string.Empty;
        var range = OrientationRange.Both;
        var symbol = string.Empty;
        var type = default(OrderType);
        var side = default(OrderSide);
        var totalQty = 0m;
        var price = 0m;
        var levelPrice = 0m;
        var reduceOnly = false;
        var status = default(OrderStatus);
        var executedQty = 0m;
        var executedPrice = 0m;
        var lastExecutedQty = 0m;
        var lastExecutedPrice = 0m;
        var commissionAmount = 0m;
        var commissionAsset = string.Empty;
        var isMaker = false;
        var transactionTime = 0L;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject && reader.CurrentDepth == currentDepth)
            {
                if (!canConvert || orderId == 0L)
                {
                    return default;
                }

                var result = new OrderUpdateEvent(
                    tradeId,
                    orderId.ToString(),
                    clientOrderId,
                    range,
                    symbol ?? string.Empty,
                    type,
                    side,
                    totalQty,
                    price,
                    levelPrice,
                    reduceOnly,
                    status,
                    executedQty,
                    executedPrice,
                    lastExecutedQty,
                    lastExecutedPrice,
                    commissionAmount,
                    commissionAsset ?? string.Empty,
                    isMaker,
                    status is OrderStatus.New ? transactionTime : 0L,
                    transactionTime
                );

                return result;
            }

            if (!canConvert)
            {
                reader.Skip();
                continue;
            }

            if (reader.TokenType == JsonTokenType.PropertyName && reader.CurrentDepth == currentDepth + 1)
            {
                var propertyName = reader.GetString();

                reader.Read();

                switch (propertyName)
                {
                    case "e":
                        var eventType = reader.GetString();
                        if (eventType != "ORDER_TRADE_UPDATE")
                        {
                            canConvert = false;
                        }

                        break;
                    case "o":
                        break;

                    default:
                        reader.Skip();
                        break;
                }
            }

            if (reader.TokenType == JsonTokenType.PropertyName && reader.CurrentDepth == currentDepth + 2)
            {
                var propertyName = reader.GetString();

                reader.Read();

                switch (propertyName)
                {
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
                        clientOrderId = reader.GetString() ?? string.Empty;
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
                    case "sp":
                        levelPrice = reader.GetDecimalFromString();
                        break;
                    case "R":
                        reduceOnly = reader.GetBoolean();
                        break;
                    case "X":
                        status = OrderStatuses.StringToValue.MapValue(reader.GetString());
                        break;
                    case "z":
                        executedQty = reader.GetDecimalFromString();
                        break;
                    case "ap":
                        executedPrice = reader.GetDecimalFromString();
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
                    case "T":
                        transactionTime = reader.GetInt64();
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
    /// Not supported: order update events are only ever read from the exchange, never written.
    /// </summary>
    /// <param name="writer">The UTF-8 JSON writer.</param>
    /// <param name="value">The event to write.</param>
    /// <param name="options">The serializer options in effect.</param>
    public override void Write(Utf8JsonWriter writer, OrderUpdateEvent? value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
