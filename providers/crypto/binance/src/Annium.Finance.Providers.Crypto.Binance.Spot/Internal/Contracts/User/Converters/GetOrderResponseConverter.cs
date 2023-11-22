using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Annium.Finance.Providers.Abstractions.Domain.Enums;
using Annium.Finance.Providers.Crypto.Binance.Spot.Internal.Contracts.User.Domain;
using Annium.Linq;
using Annium.Serialization.Json;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Internal.Contracts.User.Converters;

internal class GetOrderResponseConverter : JsonConverter<OrderResponse?>
{
    public override OrderResponse? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("deserialization failed");

        var currentDepth = reader.CurrentDepth;

        var id = Guid.Empty;
        var orderId = string.Empty;
        var symbol = string.Empty;
        var type = default(OrderType);
        var side = default(OrderSide);
        var totalQty = 0m;
        var price = 0m;
        var levelPrice = 0m;
        var status = default(OrderStatus);
        var executedQty = 0m;
        var executedSum = 0m;
        var createdAt = 0L;
        var updatedAt = 0L;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject && reader.CurrentDepth == currentDepth)
            {
                if (id == Guid.Empty || string.IsNullOrWhiteSpace(symbol))
                {
                    return default;
                }

                var executedPrice = executedQty != 0 ? executedSum / executedQty : 0m;
                var order = new OrderResponse(
                    id,
                    orderId,
                    symbol,
                    side,
                    type,
                    totalQty,
                    price,
                    levelPrice,
                    createdAt,
                    status,
                    executedQty,
                    executedPrice,
                    updatedAt
                );

                return order;
            }

            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                var propertyName = reader.GetString();

                reader.Read();

                switch (propertyName)
                {
                    case "clientOrderId":
                        id = reader.TryGetGuid(out var guid) ? guid : Guid.Empty;
                        break;
                    case "orderId":
                        orderId = reader.GetInt64().ToString();
                        break;
                    case "symbol":
                        symbol = reader.GetString();
                        break;
                    case "type":
                        type = OrderTypes.StringToValue.MapValue(reader.GetString());
                        break;
                    case "side":
                        side = OrderSides.StringToValue.MapValue(reader.GetString());
                        break;
                    case "origQty":
                        totalQty = reader.GetDecimalFromString();
                        break;
                    case "price":
                        price = reader.GetDecimalFromString();
                        break;
                    case "stopPrice":
                        levelPrice = reader.GetDecimalFromString();
                        break;
                    case "status":
                        status = OrderStatuses.StringToValue.MapValue(reader.GetString());
                        break;
                    case "executedQty":
                        executedQty = reader.GetDecimalFromString();
                        break;
                    case "cummulativeQuoteQty":
                        executedSum = reader.GetDecimalFromString();
                        break;
                    case "time":
                        createdAt = reader.GetInt64();
                        break;
                    case "updateTime":
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

    public override void Write(Utf8JsonWriter writer, OrderResponse? value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
