using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Annium.Finance.Providers.Abstractions.Domain.User;
using Annium.Serialization.Json;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Internal.User.Contracts.Converters;

/// <summary>Deserializes a Binance get-account response into its <c>balances</c> array of <see cref="AssetModel"/>.</summary>
internal class GetAccountResponseConverter : JsonConverter<IReadOnlyCollection<AssetModel>>
{
    /// <summary>Serializer options used to deserialize each entry of the <c>balances</c> array.</summary>
    private static readonly JsonSerializerOptions _balanceOptions = new JsonSerializerOptions()
        .ResetConverters()
        .AddConverter<GetAccountResponseBalanceConverter>();

    /// <summary>Reads a Binance get-account response and converts its <c>balances</c> array into a collection of <see cref="AssetModel"/>.</summary>
    /// <param name="reader">The reader positioned at the start of the response object.</param>
    /// <param name="typeToConvert">The type being converted.</param>
    /// <param name="options">The active serializer options.</param>
    /// <returns>The account's balances, or null if the response has no <c>balances</c> field.</returns>
    public override IReadOnlyCollection<AssetModel>? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("deserialization failed");

        var currentDepth = reader.CurrentDepth;

        IReadOnlyCollection<AssetModel>? balances = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject && reader.CurrentDepth == currentDepth)
            {
                return balances;
            }

            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                var propertyName = reader.GetString();

                reader.Read();

                switch (propertyName)
                {
                    case "balances":
                        balances = JsonSerializer.Deserialize<IReadOnlyCollection<AssetModel>>(
                            ref reader,
                            _balanceOptions
                        );
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }
        }

        throw new JsonException("Unexpected end of json");
    }

    /// <summary>Not supported; account balances are only ever read from Binance, never written.</summary>
    /// <param name="writer">The writer to serialize to.</param>
    /// <param name="value">The value to serialize.</param>
    /// <param name="options">The active serializer options.</param>
    public override void Write(
        Utf8JsonWriter writer,
        IReadOnlyCollection<AssetModel> value,
        JsonSerializerOptions options
    )
    {
        throw new NotImplementedException();
    }
}
