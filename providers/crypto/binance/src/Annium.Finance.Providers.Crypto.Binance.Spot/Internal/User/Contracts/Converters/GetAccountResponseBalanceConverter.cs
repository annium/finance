using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Annium.Finance.Providers.Abstractions.Domain.User;
using Annium.Serialization.Json;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Internal.User.Contracts.Converters;

/// <summary>Deserializes a single balance entry of a Binance get-account response into an <see cref="AssetModel"/>.</summary>
internal class GetAccountResponseBalanceConverter : JsonConverter<AssetModel>
{
    /// <summary>Reads a Binance account balance entry and converts it into an <see cref="AssetModel"/>.</summary>
    /// <param name="reader">The reader positioned at the start of the balance object.</param>
    /// <param name="typeToConvert">The type being converted.</param>
    /// <param name="options">The active serializer options.</param>
    /// <returns>The converted balance.</returns>
    public override AssetModel Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("deserialization failed");

        var currentDepth = reader.CurrentDepth;

        var asset = string.Empty;
        var free = 0m;
        var locked = 0m;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject && reader.CurrentDepth == currentDepth)
            {
                var balance = new AssetModel(asset, free, locked);

                return balance;
            }

            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                var propertyName = reader.GetString();

                reader.Read();

                switch (propertyName)
                {
                    case "asset":
                        asset = reader.GetString().NotNull();
                        break;
                    case "free":
                        free = reader.GetDecimalFromString();
                        break;
                    case "locked":
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

    /// <summary>Not supported; balances are only ever read from Binance, never written.</summary>
    /// <param name="writer">The writer to serialize to.</param>
    /// <param name="value">The value to serialize.</param>
    /// <param name="options">The active serializer options.</param>
    public override void Write(Utf8JsonWriter writer, AssetModel value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
