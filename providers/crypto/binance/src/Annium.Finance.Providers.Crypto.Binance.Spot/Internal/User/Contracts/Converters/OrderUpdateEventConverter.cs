using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Annium.Finance.Providers.Abstractions.Domain.User;
using Annium.Finance.Providers.Crypto.Binance.Spot.Internal.User.Contracts.Domain;
using Annium.Linq;
using Annium.Serialization.Json;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Internal.User.Contracts.Converters;

internal class OrderUpdateEventConverter : JsonConverter<OrderUpdateEvent?>
{
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

    public override void Write(Utf8JsonWriter writer, OrderUpdateEvent? value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
