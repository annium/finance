using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Annium.Finance.Providers.Abstractions.Domain.User;
using Annium.Finance.Providers.Crypto.Binance.Spot.Internal.User.Contracts.Domain;
using Annium.Linq;
using Annium.Serialization.Json;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Internal.User.Contracts.Converters;

/// <summary>Deserializes a Binance get-order response into an <see cref="OrderModel"/>.</summary>
internal class GetOrderResponseConverter : JsonConverter<OrderModel?>
{
    /// <summary>Reads a Binance get-order response and converts it into an <see cref="OrderModel"/>.</summary>
    /// <param name="reader">The reader positioned at the start of the response object.</param>
    /// <param name="typeToConvert">The type being converted.</param>
    /// <param name="options">The active serializer options.</param>
    /// <returns>The converted order, or null if the order id, client order id or symbol are missing.</returns>
    public override OrderModel? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
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
        var executedSum = 0m;
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

                var executedPrice = executedQty != 0 ? executedSum / executedQty : 0m;
                var order = new OrderModel(
                    id,
                    clientOrderId,
                    OrientationRange.Both,
                    symbol,
                    side,
                    type,
                    totalQty,
                    price,
                    levelPrice,
                    false,
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

    /// <summary>Not supported; orders are only ever read from Binance, never written.</summary>
    /// <param name="writer">The writer to serialize to.</param>
    /// <param name="value">The value to serialize.</param>
    /// <param name="options">The active serializer options.</param>
    public override void Write(Utf8JsonWriter writer, OrderModel? value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
