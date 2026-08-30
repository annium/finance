namespace Annium.Finance.Providers.Crypto.Binance.Base.User.Contracts.Domain;

/// <summary>A Binance listen key identifying the user data stream WebSocket connection to open for the account.</summary>
/// <param name="Value">The listen key value.</param>
public sealed record ListenKey(string Value);
