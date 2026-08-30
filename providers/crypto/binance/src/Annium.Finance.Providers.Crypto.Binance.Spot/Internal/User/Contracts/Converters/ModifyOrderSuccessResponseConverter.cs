using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Annium.Finance.Providers.Abstractions.Domain.User;
using Annium.Serialization.Json;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Internal.User.Contracts.Converters;

/// <summary>
/// Deserializes a successful Binance cancel-replace (modify order) response into the resulting
/// <see cref="OrderModel"/>, by unwrapping the nested <c>newOrderResponse</c> payload.
/// </summary>
internal class ModifyOrderSuccessResponseConverter : JsonConverter<OrderModel?>
{
    /// <summary>Serializer options used to deserialize the nested <c>newOrderResponse</c> payload.</summary>
    private static readonly JsonSerializerOptions _orderResponseDeserializerOptions = new JsonSerializerOptions()
        .ResetConverters()
        .AddConverter<InitOrderResponseConverter>();

    /// <summary>Reads a Binance cancel-replace response and converts its <c>newOrderResponse</c> payload into an <see cref="OrderModel"/>.</summary>
    /// <param name="reader">The reader positioned at the start of the response object.</param>
    /// <param name="typeToConvert">The type being converted.</param>
    /// <param name="options">The active serializer options.</param>
    /// <returns>The resulting order, or null if the response has no <c>newOrderResponse</c> field.</returns>
    public override OrderModel? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("deserialization failed");

        var currentDepth = reader.CurrentDepth;

        OrderModel? order = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject && reader.CurrentDepth == currentDepth)
            {
                return order;
            }

            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                var propertyName = reader.GetString();

                reader.Read();

                switch (propertyName)
                {
                    case "newOrderResponse":
                        order = JsonSerializer.Deserialize<OrderModel?>(ref reader, _orderResponseDeserializerOptions);
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }
        }

        throw new JsonException("Unexpected end of json");
    }

    /// <summary>Not supported; orders are only ever read from Binance, never written.</summary>
    /// <param name="writer">The writer to serialize to.</param>
    /// <param name="value">The value to serialize.</param>
    /// <param name="options">The active serializer options.</param>
    public override void Write(Utf8JsonWriter writer, OrderModel? value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
