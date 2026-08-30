using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Annium.Finance.Providers.Crypto.Binance.Base.User.Contracts.Domain;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Internal.User.Contracts.Converters;

/// <summary>
/// Deserializes a Binance cancel order response into a <see cref="CancelOrderResponse"/>, reading the
/// client-assigned order id as a GUID.
/// </summary>
internal class CancelOrderResponseConverter : JsonConverter<CancelOrderResponse?>
{
    /// <summary>Reads a Binance cancel order response and converts it into a <see cref="CancelOrderResponse"/>.</summary>
    /// <param name="reader">The reader positioned at the start of the response object.</param>
    /// <param name="typeToConvert">The type being converted.</param>
    /// <param name="options">The active serializer options.</param>
    /// <returns>The converted response, or null if the client order id or order id are missing or invalid.</returns>
    public override CancelOrderResponse? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("deserialization failed");

        var currentDepth = reader.CurrentDepth;

        var id = Guid.Empty;
        var orderId = string.Empty;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject && reader.CurrentDepth == currentDepth)
            {
                if (id == Guid.Empty || string.IsNullOrWhiteSpace(orderId))
                    return default;
                return new CancelOrderResponse(id, orderId);
            }

            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                var propertyName = reader.GetString();

                reader.Read();

                switch (propertyName)
                {
                    case "clientOrderId":
                        id = reader.TryGetGuid(out var guid) ? guid : Guid.Empty;
                        break;
                    case "orderId":
                        orderId = reader.GetInt64().ToString();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }
        }

        throw new JsonException("Unexpected end of json");
    }

    /// <summary>Not supported; cancel order responses are only ever read from Binance, never written.</summary>
    /// <param name="writer">The writer to serialize to.</param>
    /// <param name="value">The value to serialize.</param>
    /// <param name="options">The active serializer options.</param>
    public override void Write(Utf8JsonWriter writer, CancelOrderResponse? value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
