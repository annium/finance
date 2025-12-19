using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Annium.Finance.Providers.Abstractions.Domain.User;
using Annium.Serialization.Json;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Internal.User.Contracts.Converters;

internal class GetAccountResponseConverter : JsonConverter<IReadOnlyCollection<AssetModel>>
{
    private static readonly JsonSerializerOptions _balanceOptions = new JsonSerializerOptions()
        .ResetConverters()
        .AddConverter<GetAccountResponseBalanceConverter>();

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

    public override void Write(
        Utf8JsonWriter writer,
        IReadOnlyCollection<AssetModel> value,
        JsonSerializerOptions options
    )
    {
        throw new NotImplementedException();
    }
}
