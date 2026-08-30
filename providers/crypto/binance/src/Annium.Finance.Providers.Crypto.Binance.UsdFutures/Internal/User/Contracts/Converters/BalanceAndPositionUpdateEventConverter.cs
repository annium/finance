using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.User.Contracts.Domain;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.User.Contracts.Converters;

/// <summary>
/// Reads a user data stream <c>ACCOUNT_UPDATE</c> event into a <see cref="BalanceAndPositionUpdateEvent"/>,
/// delegating individual balances and positions to their own converters. Writing is not supported since this
/// contract is read-only (server-to-client).
/// </summary>
internal class BalanceAndPositionUpdateEventConverter : JsonConverter<BalanceAndPositionUpdateEvent?>
{
    /// <summary>
    /// Reads the event, rejecting the payload unless its <c>e</c> field is <c>ACCOUNT_UPDATE</c>.
    /// </summary>
    /// <param name="reader">The UTF-8 JSON reader positioned at the start of the event object.</param>
    /// <param name="typeToConvert">The type being converted.</param>
    /// <param name="options">The serializer options in effect.</param>
    /// <returns>The parsed event, or null if the payload is not a balance and position update event.</returns>
    public override BalanceAndPositionUpdateEvent? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException($"Expected {JsonTokenType.StartObject}, got {reader.TokenType}");

        var currentDepth = reader.CurrentDepth;

        var canConvert = true;
        var date = 0L;
        IReadOnlyCollection<BalanceAndPositionUpdateEventBalance>? balances = null;
        IReadOnlyCollection<BalanceAndPositionUpdateEventPosition>? positions = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject && reader.CurrentDepth == currentDepth)
            {
                if (!canConvert || balances is null || positions is null)
                {
                    return default;
                }

                var result = new BalanceAndPositionUpdateEvent(date, balances, positions);

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
                        if (eventType != "ACCOUNT_UPDATE")
                        {
                            canConvert = false;
                        }

                        break;
                    case "T":
                        date = reader.GetInt64();
                        break;
                    case "a":
                        break;
                    case "B":
                        balances = JsonSerializer.Deserialize<
                            IReadOnlyCollection<BalanceAndPositionUpdateEventBalance>
                        >(ref reader, options);
                        break;
                    case "P":
                        positions = JsonSerializer.Deserialize<
                            IReadOnlyCollection<BalanceAndPositionUpdateEventPosition>
                        >(ref reader, options);
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
    /// Not supported: balance and position update events are only ever read from the exchange, never written.
    /// </summary>
    /// <param name="writer">The UTF-8 JSON writer.</param>
    /// <param name="value">The event to write.</param>
    /// <param name="options">The serializer options in effect.</param>
    public override void Write(
        Utf8JsonWriter writer,
        BalanceAndPositionUpdateEvent? value,
        JsonSerializerOptions options
    )
    {
        throw new NotImplementedException();
    }
}
