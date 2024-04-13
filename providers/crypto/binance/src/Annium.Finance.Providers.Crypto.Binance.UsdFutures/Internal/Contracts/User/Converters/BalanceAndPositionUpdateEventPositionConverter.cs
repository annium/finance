using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Annium.Finance.Providers.Abstractions.Domain.Enums;
using Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Contracts.User.Domain;
using Annium.Linq;
using Annium.Serialization.Json;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Contracts.User.Converters;

internal class BalanceAndPositionUpdateEventPositionConverter : JsonConverter<BalanceAndPositionUpdateEventPosition>
{
    public override BalanceAndPositionUpdateEventPosition Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException($"Expected {JsonTokenType.StartObject}, got {reader.TokenType}");

        var currentDepth = reader.CurrentDepth;

        var symbol = string.Empty;
        var direction = OrientationRange.Both;
        var marginType = MarginType.Isolated;
        var isolatedWallet = 0m;
        var amount = 0m;
        var averagePrice = 0m;
        var unrealizedPnl = 0m;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject && reader.CurrentDepth == currentDepth)
            {
                var result = new BalanceAndPositionUpdateEventPosition(
                    symbol,
                    direction,
                    marginType,
                    isolatedWallet,
                    amount,
                    averagePrice,
                    unrealizedPnl
                );

                return result;
            }

            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                var propertyName = reader.GetString();

                reader.Read();

                switch (propertyName)
                {
                    case "s":
                        symbol = reader.GetString() ?? string.Empty;
                        break;
                    case "ps":
                        direction = OrientationRanges.StringToValue.MapValue(reader.GetString());
                        break;
                    case "mt":
                        marginType = MarginTypes.StringToValue.MapValue(reader.GetString());
                        break;
                    case "iw":
                        isolatedWallet = reader.GetDecimalFromString();
                        break;
                    case "pa":
                        amount = reader.GetDecimalFromString();
                        break;
                    case "ep":
                        averagePrice = reader.GetDecimalFromString();
                        break;
                    case "up":
                        unrealizedPnl = reader.GetDecimalFromString();
                        break;

                    default:
                        reader.Skip();
                        break;
                }
            }
        }

        throw new JsonException("Unexpected end of json");
    }

    public override void Write(
        Utf8JsonWriter writer,
        BalanceAndPositionUpdateEventPosition value,
        JsonSerializerOptions options
    )
    {
        throw new NotImplementedException();
    }
}
