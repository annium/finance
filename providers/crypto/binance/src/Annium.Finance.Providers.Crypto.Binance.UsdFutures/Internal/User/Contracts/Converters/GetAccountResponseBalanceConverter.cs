using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.User.Contracts.Domain;
using Annium.Serialization.Json;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.User.Contracts.Converters;

/// <summary>
/// Reads a single asset entry (<c>assets</c> array item) of the <c>GET /fapi/v2/account</c> response into an
/// <see cref="AccountResponseBalance"/>. Writing is not supported since this contract is read-only
/// (server-to-client).
/// </summary>
internal class GetAccountResponseBalanceConverter : JsonConverter<AccountResponseBalance>
{
    /// <summary>
    /// Reads an asset balance entry.
    /// </summary>
    /// <param name="reader">The UTF-8 JSON reader positioned at the start of the asset object.</param>
    /// <param name="typeToConvert">The type being converted.</param>
    /// <param name="options">The serializer options in effect.</param>
    /// <returns>The parsed balance.</returns>
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

    /// <summary>
    /// Not supported: balance entries are only ever read from the exchange, never written.
    /// </summary>
    /// <param name="writer">The UTF-8 JSON writer.</param>
    /// <param name="value">The balance to write.</param>
    /// <param name="options">The serializer options in effect.</param>
    public override void Write(Utf8JsonWriter writer, AccountResponseBalance value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
