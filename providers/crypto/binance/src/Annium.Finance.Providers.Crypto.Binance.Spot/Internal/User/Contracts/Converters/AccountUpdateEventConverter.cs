using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Annium.Finance.Providers.Crypto.Binance.Spot.Internal.User.Contracts.Domain;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Internal.User.Contracts.Converters;

/// <summary>
/// Deserializes a Binance user data stream event into an <see cref="AccountUpdateEvent"/>, matching only events
/// whose <c>e</c> field is <c>outboundAccountPosition</c>.
/// </summary>
internal class AccountUpdateEventConverter : JsonConverter<AccountUpdateEvent?>
{
    /// <summary>Reads a Binance user data stream event and converts it into an <see cref="AccountUpdateEvent"/>.</summary>
    /// <param name="reader">The reader positioned at the start of the event object.</param>
    /// <param name="typeToConvert">The type being converted.</param>
    /// <param name="options">The active serializer options.</param>
    /// <returns>The converted event, or null if the event is not an <c>outboundAccountPosition</c> event or is missing required fields.</returns>
    public override AccountUpdateEvent? Read(
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
        IReadOnlyCollection<AccountUpdateEventBalance>? balances = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject && reader.CurrentDepth == currentDepth)
            {
                if (!canConvert || balances is null)
                {
                    return default;
                }

                var result = new AccountUpdateEvent(date, balances);

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
                        if (eventType != "outboundAccountPosition")
                        {
                            canConvert = false;
                        }

                        break;
                    case "u":
                        date = reader.GetInt64();
                        break;
                    case "B":
                        balances = JsonSerializer.Deserialize<IReadOnlyCollection<AccountUpdateEventBalance>>(
                            ref reader,
                            options
                        );
                        break;

                    default:
                        reader.Skip();
                        break;
                }
            }
        }

        throw new JsonException("Unexpected end of json");
    }

    /// <summary>Not supported; account update events are only ever read from the Binance user data stream.</summary>
    /// <param name="writer">The writer to serialize to.</param>
    /// <param name="value">The value to serialize.</param>
    /// <param name="options">The active serializer options.</param>
    public override void Write(Utf8JsonWriter writer, AccountUpdateEvent? value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
