using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Annium.Finance.Providers.Abstractions.Domain.Enums;
using Annium.Finance.Providers.Crypto.Binance.Spot.Internal.Contracts.User.Domain;
using Annium.Linq;
using Annium.Serialization.Json;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Internal.Contracts.User.Converters;

internal class OrderUpdateEventConverter : JsonConverter<OrderUpdateEvent>
{
    public override OrderUpdateEvent Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Read failed");
        }

        var currentDepth = reader.CurrentDepth;

        var canConvert = true;
        var symbol = string.Empty;
        var tradeId = string.Empty;
        var orderId = 0L;
        var clientOrderId = string.Empty;
        var type = default(OrderType);
        var side = default(OrderSide);
        var quantity = 0m;
        var price = 0m;
        var triggerPrice = 0m;
        var status = default(OrderStatus);
        var executedQuantity = 0m;
        var executedSum = 0m;
        var lastExecutedQuantity = 0m;
        var lastExecutedPrice = 0m;
        var commissionAmount = 0m;
        var commissionAsset = string.Empty;
        var isMaker = false;
        var createdDate = 0L;
        var updatedDate = 0L;

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
                    type,
                    side,
                    quantity,
                    price,
                    triggerPrice,
                    status,
                    executedQuantity,
                    executedQuantity == 0 ? 0 : executedSum / executedQuantity,
                    lastExecutedQuantity,
                    lastExecutedPrice,
                    commissionAmount,
                    commissionAsset ?? string.Empty,
                    isMaker,
                    createdDate,
                    updatedDate
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
                        quantity = reader.GetDecimalFromString();
                        break;
                    case "p":
                        price = reader.GetDecimalFromString();
                        break;
                    case "P":
                        triggerPrice = reader.GetDecimalFromString();
                        break;
                    case "X":
                        status = OrderStatuses.StringToValue.MapValue(reader.GetString());
                        break;
                    case "z":
                        executedQuantity = reader.GetDecimalFromString();
                        break;
                    case "Z":
                        executedSum = reader.GetDecimalFromString();
                        break;
                    case "l":
                        lastExecutedQuantity = reader.GetDecimalFromString();
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
                        createdDate = reader.GetInt64();
                        break;
                    case "T":
                        updatedDate = reader.GetInt64();
                        break;

                    default:
                        reader.Skip();
                        break;
                }
            }
        }

        throw new JsonException("Unexpected end of json");
    }

    public override void Write(Utf8JsonWriter writer, OrderUpdateEvent value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("e", "executionReport");
        writer.WriteString("s", value.Symbol);
        writer.WriteNumber("t", long.Parse(value.TradeId));
        writer.WriteNumber("i", long.Parse(value.OrderId));
        writer.WriteString("c", value.ClientOrderId);
        writer.WriteString("o", OrderTypes.ValueToString[value.Type]);
        writer.WriteString("S", OrderSides.ValueToString[value.Side]);
        writer.WriteNumberString("q", value.Quantity);
        writer.WriteNumberString("p", value.Price);
        writer.WriteNumberString("P", value.TriggerPrice);
        writer.WriteString("X", OrderStatuses.ValueToString[value.Status]);
        writer.WriteNumberString("z", value.ExecutedQuantity);
        writer.WriteNumberString("Z", value.ExecutedQuantity * value.ExecutedPrice);
        writer.WriteNumberString("l", value.LastExecutedQuantity);
        writer.WriteNumberString("L", value.LastExecutedPrice);
        writer.WriteNumberString("n", value.CommissionAmount);
        writer.WriteString("N", value.CommissionAsset);
        writer.WriteBoolean("m", value.IsMaker);
        writer.WriteNumber("O", value.CreatedDate);
        writer.WriteNumber("T", value.UpdatedDate);

        writer.WriteEndObject();
    }
}
