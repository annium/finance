using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Annium.Finance.Providers.Crypto.Binance.Spot.Internal.User.Contracts.Domain;
using Annium.Serialization.Json;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Internal.User.Contracts.Converters;

/// <summary>Deserializes a single balance entry (<c>B</c> array element) of an <c>outboundAccountPosition</c> event into an <see cref="AccountUpdateEventBalance"/>.</summary>
internal class AccountUpdateEventBalanceConverter : JsonConverter<AccountUpdateEventBalance>
{
    /// <summary>Reads a Binance balance entry and converts it into an <see cref="AccountUpdateEventBalance"/>.</summary>
    /// <param name="reader">The reader positioned at the start of the balance object.</param>
    /// <param name="typeToConvert">The type being converted.</param>
    /// <param name="options">The active serializer options.</param>
    /// <returns>The converted balance.</returns>
    public override AccountUpdateEventBalance Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException($"Expected {JsonTokenType.StartObject}, got {reader.TokenType}");

        var currentDepth = reader.CurrentDepth;

        var asset = string.Empty;
        var free = 0m;
        var locked = 0m;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject && reader.CurrentDepth == currentDepth)
            {
                var result = new AccountUpdateEventBalance(asset, free, locked);

                return result;
            }

            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                var propertyName = reader.GetString();

                reader.Read();

                switch (propertyName)
                {
                    case "a":
                        asset = reader.GetString() ?? string.Empty;
                        break;
                    case "f":
                        free = reader.GetDecimalFromString();
                        break;
                    case "l":
                        locked = reader.GetDecimalFromString();
                        break;

                    default:
                        reader.Skip();
                        break;
                }
            }
        }

        throw new JsonException("Unexpected end of json");
    }

    /// <summary>Not supported; balance entries are only ever read from the Binance user data stream.</summary>
    /// <param name="writer">The writer to serialize to.</param>
    /// <param name="value">The value to serialize.</param>
    /// <param name="options">The active serializer options.</param>
    public override void Write(Utf8JsonWriter writer, AccountUpdateEventBalance value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
