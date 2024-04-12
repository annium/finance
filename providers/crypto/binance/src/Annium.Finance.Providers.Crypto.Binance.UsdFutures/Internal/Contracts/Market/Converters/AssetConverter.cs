using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Contracts.Market.Domain;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Contracts.Market.Converters;

internal class AssetConverter : JsonConverter<Asset?>
{
    public override Asset? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException($"Expected {JsonTokenType.StartObject}, got {reader.TokenType}");

        var currentDepth = reader.CurrentDepth;

        var code = string.Empty;
        var marginAvailable = false;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject && reader.CurrentDepth == currentDepth)
            {
                if (code.IsNullOrWhiteSpace() || !marginAvailable)
                {
                    return default;
                }

                return new Asset(code);
            }

            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                var propertyName = reader.GetString();

                reader.Read();

                switch (propertyName)
                {
                    case "asset":
                        code = reader.GetString();
                        break;
                    case "marginAvailable":
                        marginAvailable = reader.GetBoolean();
                        break;

                    default:
                        reader.Skip();
                        break;
                }
            }
        }

        throw new JsonException("Unexpected end of json");
    }

    public override void Write(Utf8JsonWriter writer, Asset? value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
