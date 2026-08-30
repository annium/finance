namespace Annium.Finance.Providers.Crypto.Binance.Base.Shared.Contracts.Domain;

/// <summary>Binance's current server time, as returned by its <c>/time</c> endpoint.</summary>
/// <param name="Value">The server time in milliseconds since the Unix epoch.</param>
public sealed record ServerTime(long Value);
