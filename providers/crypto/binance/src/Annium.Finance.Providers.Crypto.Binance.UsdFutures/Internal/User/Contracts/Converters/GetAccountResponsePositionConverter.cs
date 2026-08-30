using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Annium.Finance.Providers.Abstractions.Domain.User;
using Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.User.Contracts.Domain;
using Annium.Linq;
using Annium.Serialization.Json;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.User.Contracts.Converters;

/// <summary>
/// Reads a single position entry (<c>positions</c> array item) of the <c>GET /fapi/v2/account</c> response into
/// an <see cref="AccountResponsePosition"/>, deriving the margin type from the boolean <c>isolated</c> flag.
/// Writing is not supported since this contract is read-only (server-to-client).
/// </summary>
internal class GetAccountResponsePositionConverter : JsonConverter<AccountResponsePosition>
{
    /// <summary>
    /// Reads a position entry.
    /// </summary>
    /// <param name="reader">The UTF-8 JSON reader positioned at the start of the position object.</param>
    /// <param name="typeToConvert">The type being converted.</param>
    /// <param name="options">The serializer options in effect.</param>
    /// <returns>The parsed position.</returns>
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

    /// <summary>
    /// Not supported: position entries are only ever read from the exchange, never written.
    /// </summary>
    /// <param name="writer">The UTF-8 JSON writer.</param>
    /// <param name="value">The position to write.</param>
    /// <param name="options">The serializer options in effect.</param>
    public override void Write(Utf8JsonWriter writer, AccountResponsePosition value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
