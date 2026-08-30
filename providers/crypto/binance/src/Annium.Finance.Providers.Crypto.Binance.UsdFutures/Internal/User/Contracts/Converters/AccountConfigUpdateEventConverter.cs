using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.User.Contracts.Domain;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.User.Contracts.Converters;

/// <summary>
/// Reads a user data stream <c>ACCOUNT_CONFIG_UPDATE</c> event into an <see cref="AccountConfigUpdateEvent"/>.
/// Writing is not supported since this contract is read-only (server-to-client).
/// </summary>
internal class AccountConfigUpdateEventConverter : JsonConverter<AccountConfigUpdateEvent?>
{
    /// <summary>
    /// Reads the event, rejecting the payload unless its <c>e</c> field is <c>ACCOUNT_CONFIG_UPDATE</c>.
    /// </summary>
    /// <param name="reader">The UTF-8 JSON reader positioned at the start of the event object.</param>
    /// <param name="typeToConvert">The type being converted.</param>
    /// <param name="options">The serializer options in effect.</param>
    /// <returns>The parsed event, or null if the payload is not an account config update event.</returns>
    public override AccountConfigUpdateEvent? Read(
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
        var type = AccountConfigUpdateEventType.MultiAssetsModeChange;
        var multiAssetsMode = false;
        var symbol = string.Empty;
        var leverage = 0;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject && reader.CurrentDepth == currentDepth)
            {
                if (!canConvert)
                {
                    return default;
                }

                var result = new AccountConfigUpdateEvent(
                    date,
                    type,
                    multiAssetsMode,
                    symbol ?? string.Empty,
                    leverage
                );

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
                        if (eventType != "ACCOUNT_CONFIG_UPDATE")
                        {
                            canConvert = false;
                        }

                        break;
                    case "T":
                        date = reader.GetInt64();
                        break;
                    case "ai":
                        type = AccountConfigUpdateEventType.MultiAssetsModeChange;
                        break;
                    case "ac":
                        type = AccountConfigUpdateEventType.LeverageChange;
                        break;
                    case "j":
                        multiAssetsMode = reader.GetBoolean();
                        break;
                    case "s":
                        symbol = reader.GetString();
                        break;
                    case "l":
                        leverage = reader.GetInt32();
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
    /// Not supported: account config update events are only ever read from the exchange, never written.
    /// </summary>
    /// <param name="writer">The UTF-8 JSON writer.</param>
    /// <param name="value">The event to write.</param>
    /// <param name="options">The serializer options in effect.</param>
    public override void Write(Utf8JsonWriter writer, AccountConfigUpdateEvent? value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
