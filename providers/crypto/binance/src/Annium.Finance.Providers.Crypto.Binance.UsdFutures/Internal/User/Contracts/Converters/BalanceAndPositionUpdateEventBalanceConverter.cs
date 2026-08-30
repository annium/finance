using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.User.Contracts.Domain;
using Annium.Serialization.Json;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.User.Contracts.Converters;

/// <summary>
/// Reads a single balance entry (<c>B</c> array item) of an <c>ACCOUNT_UPDATE</c> event into a
/// <see cref="BalanceAndPositionUpdateEventBalance"/>. Writing is not supported since this contract is read-only
/// (server-to-client).
/// </summary>
internal class BalanceAndPositionUpdateEventBalanceConverter : JsonConverter<BalanceAndPositionUpdateEventBalance>
{
    /// <summary>
    /// Reads a balance entry.
    /// </summary>
    /// <param name="reader">The UTF-8 JSON reader positioned at the start of the balance object.</param>
    /// <param name="typeToConvert">The type being converted.</param>
    /// <param name="options">The serializer options in effect.</param>
    /// <returns>The parsed balance entry.</returns>
    public override BalanceAndPositionUpdateEventBalance Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException($"Expected {JsonTokenType.StartObject}, got {reader.TokenType}");

        var currentDepth = reader.CurrentDepth;

        var asset = string.Empty;
        var walletBalance = 0m;
        var crossWalletBalance = 0m;
        var balanceChange = 0m;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject && reader.CurrentDepth == currentDepth)
            {
                var result = new BalanceAndPositionUpdateEventBalance(
                    asset,
                    walletBalance,
                    crossWalletBalance,
                    balanceChange
                );

                return result;
            }

            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                var propertyName = reader.GetString();

                reader.Read();

                switch (propertyName)
                {
                    case "a":
                        asset = reader.GetString() ?? string.Empty;
                        break;
                    case "wb":
                        walletBalance = reader.GetDecimalFromString();
                        break;
                    case "cw":
                        crossWalletBalance = reader.GetDecimalFromString();
                        break;
                    case "bc":
                        balanceChange = reader.GetDecimalFromString();
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
    /// <param name="value">The balance entry to write.</param>
    /// <param name="options">The serializer options in effect.</param>
    public override void Write(
        Utf8JsonWriter writer,
        BalanceAndPositionUpdateEventBalance value,
        JsonSerializerOptions options
    )
    {
        throw new NotImplementedException();
    }
}
