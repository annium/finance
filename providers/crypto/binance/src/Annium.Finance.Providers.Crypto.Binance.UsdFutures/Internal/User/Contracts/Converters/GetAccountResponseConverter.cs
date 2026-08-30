using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.User.Contracts.Domain;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.User.Contracts.Converters;

/// <summary>
/// Reads the <c>GET /fapi/v2/account</c> response into an <see cref="AccountResponse"/>, delegating individual
/// balances and positions to their own converters. Writing is not supported since this contract is read-only
/// (server-to-client).
/// </summary>
internal class GetAccountResponseConverter : JsonConverter<AccountResponse?>
{
    /// <summary>
    /// Reads the account's asset balances and positions.
    /// </summary>
    /// <param name="reader">The UTF-8 JSON reader positioned at the start of the account object.</param>
    /// <param name="typeToConvert">The type being converted.</param>
    /// <param name="options">The serializer options in effect.</param>
    /// <returns>The parsed account, or null if balances or positions could not be parsed.</returns>
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

    /// <summary>
    /// Not supported: account responses are only ever read from the exchange, never written.
    /// </summary>
    /// <param name="writer">The UTF-8 JSON writer.</param>
    /// <param name="value">The account response to write.</param>
    /// <param name="options">The serializer options in effect.</param>
    public override void Write(Utf8JsonWriter writer, AccountResponse? value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
