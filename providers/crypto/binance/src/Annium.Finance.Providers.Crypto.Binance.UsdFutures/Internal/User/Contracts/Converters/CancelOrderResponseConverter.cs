using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Annium.Finance.Providers.Crypto.Binance.Base.User.Contracts.Domain;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.User.Contracts.Converters;

/// <summary>
/// Reads the response of the cancel-order endpoint into a <see cref="CancelOrderResponse"/>, pairing the
/// client-supplied <c>clientOrderId</c> with the exchange-assigned <c>orderId</c>. Writing is not supported
/// since this contract is read-only (server-to-client).
/// </summary>
internal class CancelOrderResponseConverter : JsonConverter<CancelOrderResponse?>
{
    /// <summary>
    /// Reads the response.
    /// </summary>
    /// <param name="reader">The UTF-8 JSON reader positioned at the start of the response object.</param>
    /// <param name="typeToConvert">The type being converted.</param>
    /// <param name="options">The serializer options in effect.</param>
    /// <returns>The parsed response, or null if the client order id or order id could not be resolved.</returns>
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

    /// <summary>
    /// Not supported: cancel-order responses are only ever read from the exchange, never written.
    /// </summary>
    /// <param name="writer">The UTF-8 JSON writer.</param>
    /// <param name="value">The response to write.</param>
    /// <param name="options">The serializer options in effect.</param>
    public override void Write(Utf8JsonWriter writer, CancelOrderResponse? value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
