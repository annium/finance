using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Annium.Finance.Providers.Abstractions.Domain.User;
using Annium.Serialization.Json;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Internal.User.Contracts.Converters;

internal class GetTradeResponseConverter : JsonConverter<TradeModel?>
{
    public override TradeModel? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("deserialization failed");

        var currentDepth = reader.CurrentDepth;

        var id = string.Empty;
        var orderId = string.Empty;
        var symbol = string.Empty;
        var qty = 0m;
        var price = 0m;
        var commissionAsset = string.Empty;
        var commissionAmount = 0m;
        var isMaker = false;
        var moment = 0L;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject && reader.CurrentDepth == currentDepth)
            {
                if (
                    string.IsNullOrWhiteSpace(orderId)
                    || string.IsNullOrWhiteSpace(symbol)
                    || string.IsNullOrWhiteSpace(commissionAsset)
                )
                {
                    return default;
                }

                var trade = new TradeModel(
                    id,
                    orderId,
                    symbol,
                    qty,
                    price,
                    commissionAsset,
                    commissionAmount,
                    isMaker,
                    moment
                );

                return trade;
            }

            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                var propertyName = reader.GetString();

                reader.Read();

                switch (propertyName)
                {
                    case "id":
                        id = reader.GetInt64().ToString();
                        break;
                    case "orderId":
                        orderId = reader.GetInt64().ToString();
                        break;
                    case "symbol":
                        symbol = reader.GetString();
                        break;
                    case "qty":
                        qty = reader.GetDecimalFromString();
                        break;
                    case "price":
                        price = reader.GetDecimalFromString();
                        break;
                    case "commission":
                        commissionAmount = reader.GetDecimalFromString();
                        break;
                    case "commissionAsset":
                        commissionAsset = reader.GetString();
                        break;
                    case "isMaker":
                        isMaker = reader.GetBoolean();
                        break;
                    case "time":
                        moment = reader.GetInt64();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }
        }

        throw new JsonException("Unexpected end of json");
    }

    public override void Write(Utf8JsonWriter writer, TradeModel? value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
