using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Market.Contracts.Domain;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Market.Contracts.Converters;

/// <summary>
/// Reads a Binance exchange info asset entry into an <see cref="Asset"/>. Writing is not supported since this
/// contract is read-only (server-to-client).
/// </summary>
internal class AssetConverter : JsonConverter<Asset?>
{
    /// <summary>
    /// Reads an asset entry, keeping only its code and requiring <c>marginAvailable</c> to be set.
    /// </summary>
    /// <param name="reader">The UTF-8 JSON reader positioned at the start of the asset object.</param>
    /// <param name="typeToConvert">The type being converted.</param>
    /// <param name="options">The serializer options in effect.</param>
    /// <returns>The parsed asset, or null if the asset is not margin-available or has no code.</returns>
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

    /// <summary>
    /// Not supported: assets are only ever read from the exchange, never written.
    /// </summary>
    /// <param name="writer">The UTF-8 JSON writer.</param>
    /// <param name="value">The asset to write.</param>
    /// <param name="options">The serializer options in effect.</param>
    public override void Write(Utf8JsonWriter writer, Asset? value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
