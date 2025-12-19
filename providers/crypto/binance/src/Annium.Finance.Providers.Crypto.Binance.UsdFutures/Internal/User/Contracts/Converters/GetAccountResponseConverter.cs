using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.User.Contracts.Domain;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.User.Contracts.Converters;

internal class GetAccountResponseConverter : JsonConverter<AccountResponse?>
{
    public override AccountResponse? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("deserialization failed");

        var currentDepth = reader.CurrentDepth;

        IReadOnlyCollection<AccountResponseBalance>? balances = null;
        IReadOnlyCollection<AccountResponsePosition>? positions = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject && reader.CurrentDepth == currentDepth)
            {
                return balances is not null && positions is not null
                    ? new AccountResponse(balances, positions)
                    : default;
            }

            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                var propertyName = reader.GetString();

                reader.Read();

                switch (propertyName)
                {
                    case "assets":
                        balances = JsonSerializer.Deserialize<IReadOnlyCollection<AccountResponseBalance>>(
                            ref reader,
                            options
                        );
                        break;
                    case "positions":
                        positions = JsonSerializer.Deserialize<IReadOnlyCollection<AccountResponsePosition>>(
                            ref reader,
                            options
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

    public override void Write(Utf8JsonWriter writer, AccountResponse? value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
