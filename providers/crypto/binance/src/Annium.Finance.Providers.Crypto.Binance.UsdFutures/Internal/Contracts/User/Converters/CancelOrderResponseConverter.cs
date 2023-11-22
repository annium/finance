using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Annium.Finance.Providers.Crypto.Binance.Base.Contracts.User.Domain;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Contracts.User.Converters;

internal class CancelOrderResponseConverter : JsonConverter<CancelOrderResponse?>
{
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

    public override void Write(Utf8JsonWriter writer, CancelOrderResponse? value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
