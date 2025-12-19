using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Annium.Finance.Providers.Abstractions.Domain.User;
using Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Contracts.User.Domain;
using Annium.Linq;
using Annium.Serialization.Json;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Contracts.User.Converters;

internal class GetAccountResponsePositionConverter : JsonConverter<AccountResponsePosition>
{
    public override AccountResponsePosition Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("deserialization failed");

        var currentDepth = reader.CurrentDepth;

        var symbol = string.Empty;
        var direction = OrientationRange.Both;
        var marginType = MarginType.Isolated;
        var leverage = 0m;
        var amount = 0m;
        var averagePrice = 0m;
        var unrealizedPnl = 0m;
        var updatedDate = 0L;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject && reader.CurrentDepth == currentDepth)
            {
                var position = new AccountResponsePosition(
                    symbol,
                    direction,
                    marginType,
                    leverage,
                    amount,
                    averagePrice,
                    unrealizedPnl,
                    updatedDate
                );

                return position;
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
                    case "positionSide":
                        direction = OrientationRanges.StringToValue.MapValue(reader.GetString());
                        break;
                    case "isolated":
                        marginType = reader.GetBoolean() ? MarginType.Isolated : MarginType.Cross;
                        break;
                    case "leverage":
                        leverage = reader.GetDecimalFromString();
                        break;
                    case "positionAmt":
                        amount = reader.GetDecimalFromString();
                        break;
                    case "entryPrice":
                        averagePrice = reader.GetDecimalFromString();
                        break;
                    case "unrealizedProfit":
                        unrealizedPnl = reader.GetDecimalFromString();
                        break;
                    case "updateTime":
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

    public override void Write(Utf8JsonWriter writer, AccountResponsePosition value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
