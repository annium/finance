using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Contracts.User.Domain;
using Annium.Serialization.Json;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Contracts.User.Converters;

internal class GetAccountResponseBalanceConverter : JsonConverter<AccountResponseBalance>
{
    public override AccountResponseBalance Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("deserialization failed");

        var currentDepth = reader.CurrentDepth;

        var asset = string.Empty;
        var totalBalance = 0m;
        var availableBalance = 0m;
        var initialMargin = 0m;
        var maintMargin = 0m;
        var updatedDate = 0L;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject && reader.CurrentDepth == currentDepth)
            {
                var balance = new AccountResponseBalance(
                    asset,
                    totalBalance,
                    availableBalance,
                    initialMargin,
                    maintMargin,
                    updatedDate
                );

                return balance;
            }

            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                var propertyName = reader.GetString();

                reader.Read();

                switch (propertyName)
                {
                    case "asset":
                        asset = reader.GetString().NotNull();
                        break;
                    case "marginBalance":
                        totalBalance = reader.GetDecimalFromString();
                        break;
                    case "maxWithdrawAmount":
                        availableBalance = reader.GetDecimalFromString();
                        break;
                    case "initialMargin":
                        initialMargin = reader.GetDecimalFromString();
                        break;
                    case "maintMargin":
                        maintMargin = reader.GetDecimalFromString();
                        break;
                    case "updateTime":
                        updatedDate = reader.GetInt64();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }
        }

        throw new JsonException("Unexpected end of json");
    }

    public override void Write(Utf8JsonWriter writer, AccountResponseBalance value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
