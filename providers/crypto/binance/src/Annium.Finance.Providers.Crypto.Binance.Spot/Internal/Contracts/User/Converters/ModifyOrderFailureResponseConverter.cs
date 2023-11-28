using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Annium.Core.DependencyInjection;
using Annium.Finance.Providers.Crypto.Binance.Base.Contracts.Shared.Converters;
using Annium.Finance.Providers.Crypto.Binance.Base.Contracts.Shared.Domain;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Internal.Contracts.User.Converters;

internal class ModifyOrderFailureResponseConverter : JsonConverter<OperationResult?>
{
    private static readonly JsonSerializerOptions OperationResultDeserializerOptions = new JsonSerializerOptions()
        .ResetConverters()
        .AddConverter<OperationResultConverter>();

    public override OperationResult? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("deserialization failed");

        var currentDepth = reader.CurrentDepth;

        var code = 0L;
        var message = string.Empty;
        OperationResult? result = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject && reader.CurrentDepth == currentDepth)
            {
                if (result is not null)
                    return result;

                if (code == 0 || string.IsNullOrWhiteSpace(message))
                    return default;

                return new OperationResult(code, message);
            }

            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                var propertyName = reader.GetString();

                reader.Read();

                switch (propertyName)
                {
                    case "code":
                        code = reader.GetInt64();
                        break;
                    case "msg":
                        message = reader.GetString();
                        break;
                    case "data":
                        result = DeserializeData(ref reader);
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }
        }

        throw new JsonException("Unexpected end of json");

        static OperationResult? DeserializeData(ref Utf8JsonReader reader)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException("deserialization failed");
            }

            var currentDepth = reader.CurrentDepth;

            OperationResult? cancelResponse = null;
            OperationResult? initResponse = null;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject && reader.CurrentDepth == currentDepth)
                {
                    // this is intentional - converter expects bad result, stepping from cancel response to init response
                    return cancelResponse ?? initResponse;
                }

                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    var propertyName = reader.GetString();

                    reader.Read();

                    switch (propertyName)
                    {
                        case "cancelResponse":
                            if (reader.TokenType == JsonTokenType.StartObject)
                            {
                                cancelResponse = JsonSerializer.Deserialize<OperationResult>(
                                    ref reader,
                                    OperationResultDeserializerOptions
                                );
                            }

                            break;
                        case "newOrderResponse":
                            if (reader.TokenType == JsonTokenType.StartObject)
                            {
                                initResponse = JsonSerializer.Deserialize<OperationResult>(
                                    ref reader,
                                    OperationResultDeserializerOptions
                                );
                            }

                            break;
                        default:
                            reader.Skip();
                            break;
                    }
                }
            }

            throw new JsonException("Unexpected end of json");
        }
    }

    public override void Write(Utf8JsonWriter writer, OperationResult? value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
