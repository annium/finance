using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Annium.Finance.Providers.Abstractions.Domain.User;
using Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.User.Contracts.Domain;
using Annium.Linq;
using Annium.Serialization.Json;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.User.Contracts.Converters;

/// <summary>
/// Reads a single position entry (<c>P</c> array item) of an <c>ACCOUNT_UPDATE</c> event into a
/// <see cref="BalanceAndPositionUpdateEventPosition"/>. Writing is not supported since this contract is
/// read-only (server-to-client).
/// </summary>
internal class BalanceAndPositionUpdateEventPositionConverter : JsonConverter<BalanceAndPositionUpdateEventPosition>
{
    /// <summary>
    /// Reads a position entry, mapping the <c>ps</c> and <c>mt</c> wire codes to <see cref="OrientationRange"/>
    /// and <see cref="MarginType"/> respectively.
    /// </summary>
    /// <param name="reader">The UTF-8 JSON reader positioned at the start of the position object.</param>
    /// <param name="typeToConvert">The type being converted.</param>
    /// <param name="options">The serializer options in effect.</param>
    /// <returns>The parsed position entry.</returns>
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

    /// <summary>
    /// Not supported: position entries are only ever read from the exchange, never written.
    /// </summary>
    /// <param name="writer">The UTF-8 JSON writer.</param>
    /// <param name="value">The position entry to write.</param>
    /// <param name="options">The serializer options in effect.</param>
    public override void Write(
        Utf8JsonWriter writer,
        BalanceAndPositionUpdateEventPosition value,
        JsonSerializerOptions options
    )
    {
        throw new NotImplementedException();
    }
}
