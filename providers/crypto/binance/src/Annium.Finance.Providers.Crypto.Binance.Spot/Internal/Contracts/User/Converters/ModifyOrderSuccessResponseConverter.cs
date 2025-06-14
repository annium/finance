using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Annium.Finance.Providers.Abstractions.Domain.Models;
using Annium.Serialization.Json;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Internal.Contracts.User.Converters;

internal class ModifyOrderSuccessResponseConverter : JsonConverter<OrderModel?>
{
    private static readonly JsonSerializerOptions _orderResponseDeserializerOptions = new JsonSerializerOptions()
        .ResetConverters()
        .AddConverter<InitOrderResponseConverter>();

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

    public override void Write(Utf8JsonWriter writer, OrderModel? value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
