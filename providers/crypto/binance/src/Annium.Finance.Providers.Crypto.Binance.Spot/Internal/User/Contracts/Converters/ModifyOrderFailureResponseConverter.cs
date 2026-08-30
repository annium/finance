using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Annium.Finance.Providers.Crypto.Binance.Base.Shared.Contracts.Converters;
using Annium.Finance.Providers.Crypto.Binance.Base.Shared.Contracts.Domain;
using Annium.Serialization.Json;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Internal.User.Contracts.Converters;

/// <summary>
/// Deserializes a failed Binance cancel-replace (modify order) response into an <see cref="OperationResult"/>.
/// Binance reports a top-level error for a request-level failure, or a top-level success carrying a nested
/// <c>data.cancelResponse</c>/<c>data.newOrderResponse</c> error when only one leg of the cancel-then-replace
/// failed; this converter surfaces whichever error applies, preferring the cancel leg.
/// </summary>
internal class ModifyOrderFailureResponseConverter : JsonConverter<OperationResult?>
{
    /// <summary>Serializer options used to deserialize the nested <c>cancelResponse</c>/<c>newOrderResponse</c> error payloads.</summary>
    private static readonly JsonSerializerOptions _operationResultDeserializerOptions = new JsonSerializerOptions()
        .ResetConverters()
        .AddConverter<OperationResultConverter>();

    /// <summary>Reads a Binance cancel-replace response and converts its failure, if any, into an <see cref="OperationResult"/>.</summary>
    /// <param name="reader">The reader positioned at the start of the response object.</param>
    /// <param name="typeToConvert">The type being converted.</param>
    /// <param name="options">The active serializer options.</param>
    /// <returns>The applicable error, or null if the response carries no recognizable error.</returns>
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
                                    _operationResultDeserializerOptions
                                );
                            }

                            break;
                        case "newOrderResponse":
                            if (reader.TokenType == JsonTokenType.StartObject)
                            {
                                initResponse = JsonSerializer.Deserialize<OperationResult>(
                                    ref reader,
                                    _operationResultDeserializerOptions
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

    /// <summary>Not supported; modify order failures are only ever read from Binance, never written.</summary>
    /// <param name="writer">The writer to serialize to.</param>
    /// <param name="value">The value to serialize.</param>
    /// <param name="options">The active serializer options.</param>
    public override void Write(Utf8JsonWriter writer, OperationResult? value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
