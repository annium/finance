using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Annium.Core.DependencyInjection;
using Annium.Finance.Providers.Abstractions.Domain.Dto;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Internal.Contracts.User.Converters;

internal class ModifyOrderSuccessResponseConverter : JsonConverter<OrderDto?>
{
    private static readonly JsonSerializerOptions OrderResponseDeserializerOptions = new JsonSerializerOptions()
        .ResetConverters()
        .AddConverter<InitOrderResponseConverter>();

    public override OrderDto? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("deserialization failed");

        var currentDepth = reader.CurrentDepth;

        OrderDto? order = null;

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
                        order = JsonSerializer.Deserialize<OrderDto?>(ref reader, OrderResponseDeserializerOptions);
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }
        }

        throw new JsonException("Unexpected end of json");
    }

    public override void Write(Utf8JsonWriter writer, OrderDto? value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
