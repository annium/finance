using Annium.Finance.Providers.Crypto.Binance.Base.Market;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Market;

/// <summary>
/// Resolved market connector configuration (endpoints, provider key, environment) for USD-M futures. Has no
/// fields of its own; it exists to give the base configuration a concrete, per-provider type.
/// </summary>
internal sealed record MarketConfig : MarketConfigBase;
