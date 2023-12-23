using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Contracts.User.Domain;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Contracts.User.Converters;

internal class AccountConfigUpdateEventConverter : JsonConverter<AccountConfigUpdateEvent?>
{
    public override AccountConfigUpdateEvent? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Read failed");
        }

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

    public override void Write(
        Utf8JsonWriter writer,
        AccountConfigUpdateEvent? value,
        JsonSerializerOptions options
    )
    {
        throw new NotImplementedException();
    }
}
