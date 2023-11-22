using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Annium.Core.DependencyInjection;
using Annium.Finance.Providers.Abstractions.Domain.Dto;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Internal.Contracts.User.Converters;

internal class GetAccountResponseConverter : JsonConverter<IReadOnlyCollection<AssetDto>>
{
    private static readonly JsonSerializerOptions BalanceOptions = new JsonSerializerOptions()
        .ResetConverters()
        .AddConverter<GetAccountResponseBalanceConverter>();

    public override IReadOnlyCollection<AssetDto>? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("deserialization failed");

        var currentDepth = reader.CurrentDepth;

        IReadOnlyCollection<AssetDto>? balances = null;

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
                        balances = JsonSerializer.Deserialize<IReadOnlyCollection<AssetDto>>(
                            ref reader,
                            BalanceOptions
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
        IReadOnlyCollection<AssetDto> value,
        JsonSerializerOptions options
    )
    {
        throw new NotImplementedException();
    }
}
