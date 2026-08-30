using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Annium.Finance.Providers.Abstractions.Domain.User;
using Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.User.Contracts.Domain;
using Annium.Linq;
using Annium.Serialization.Json;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.User.Contracts.Converters;

/// <summary>
/// Reads the response of the order lookup endpoint (<c>GET /fapi/v1/order</c> and open/all order list variants)
/// into an <see cref="OrderModel"/>. <c>stopPrice</c> is mapped to the order's trigger/level price, and
/// <c>reduceOnly</c> indicates the order may only reduce an existing position, never open or flip it. Writing is
/// not supported since this contract is read-only (server-to-client).
/// </summary>
internal class GetOrderResponseConverter : JsonConverter<OrderModel?>
{
    /// <summary>
    /// Reads an order entry.
    /// </summary>
    /// <param name="reader">The UTF-8 JSON reader positioned at the start of the order object.</param>
    /// <param name="typeToConvert">The type being converted.</param>
    /// <param name="options">The serializer options in effect.</param>
    /// <returns>The parsed order, or null if the order id, client order id or symbol is missing.</returns>
    public override OrderModel? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("deserialization failed");

        var currentDepth = reader.CurrentDepth;

        var id = string.Empty;
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

                var order = new OrderModel(
                    id,
                    clientOrderId,
                    range,
                    symbol,
                    side,
                    type,
                    totalQty,
                    price,
                    levelPrice,
                    reduceOnly,
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
                    case "positionSide":
                        range = OrientationRanges.StringToValue.MapValue(reader.GetString());
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
                    case "reduceOnly":
                        reduceOnly = reader.GetBoolean();
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

    /// <summary>
    /// Not supported: orders are only ever read from the exchange, never written.
    /// </summary>
    /// <param name="writer">The UTF-8 JSON writer.</param>
    /// <param name="value">The order to write.</param>
    /// <param name="options">The serializer options in effect.</param>
    public override void Write(Utf8JsonWriter writer, OrderModel? value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
