namespace Annium.Finance.Providers.Crypto.Binance.Base.Market.Contracts.Domain;

/// <summary>Binance's request weight limit for the market HTTP API, as reported by the <c>REQUEST_WEIGHT</c> entry of the <c>exchangeInfo</c> response.</summary>
/// <param name="RequestWeightLimit">The maximum request weight allowed within the 1-minute interval.</param>
public sealed record RateLimits(int RequestWeightLimit);
