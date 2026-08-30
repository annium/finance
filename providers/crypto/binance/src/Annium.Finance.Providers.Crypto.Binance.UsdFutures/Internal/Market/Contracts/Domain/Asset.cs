namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Market.Contracts.Domain;

/// <summary>
/// A margin-eligible asset as reported by the exchange info endpoint. Only assets with <c>marginAvailable</c> set
/// are kept, since those are the ones usable as multi-assets margin collateral.
/// </summary>
/// <param name="Code">The asset code (e.g. <c>USDT</c>, <c>BUSD</c>).</param>
internal sealed record Asset(string Code);
