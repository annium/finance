using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.User.Contracts.Domain;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.User.Contracts.Converters;

internal class BalanceAndPositionUpdateEventConverter : JsonConverter<BalanceAndPositionUpdateEvent?>
{
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

    public override void Write(
        Utf8JsonWriter writer,
        BalanceAndPositionUpdateEvent? value,
        JsonSerializerOptions options
    )
    {
        throw new NotImplementedException();
    }
}
