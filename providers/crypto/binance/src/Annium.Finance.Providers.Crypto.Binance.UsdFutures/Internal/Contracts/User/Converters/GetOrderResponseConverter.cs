using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Annium.Finance.Providers.Abstractions.Domain.Dto;
using Annium.Finance.Providers.Abstractions.Domain.Enums;
using Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Contracts.User.Domain;
using Annium.Linq;
using Annium.Serialization.Json;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Contracts.User.Converters;

internal class GetOrderResponseConverter : JsonConverter<OrderDto?>
{
    public override OrderDto? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("deserialization failed");

        var currentDepth = reader.CurrentDepth;

        var id = string.Empty;
        var clientOrderId = string.Empty;
        var symbol = string.Empty;
        var type = default(OrderType);
        var side = default(OrderSide);
        var totalQty = 0m;
        var price = 0m;
        var levelPrice = 0m;
        var status = default(OrderStatus);
        var executedQty = 0m;
        var executedPrice = 0m;
        var createdAt = 0L;
        var updatedAt = 0L;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject && reader.CurrentDepth == currentDepth)
            {
                if (id.IsNullOrWhiteSpace() || clientOrderId.IsNullOrWhiteSpace() || symbol.IsNullOrWhiteSpace())
                {
                    return default;
                }

                var order = new OrderDto(
                    id,
                    clientOrderId,
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
                    case "orderId":
                        id = reader.GetInt64().ToString();
                        break;
                    case "clientOrderId":
                        clientOrderId = reader.GetString();
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
                    case "avgPrice":
                        executedPrice = reader.GetDecimalFromString();
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

    public override void Write(Utf8JsonWriter writer, OrderDto? value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
