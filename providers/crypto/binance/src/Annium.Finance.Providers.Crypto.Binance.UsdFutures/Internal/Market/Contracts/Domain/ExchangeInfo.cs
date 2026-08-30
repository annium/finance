using System.Collections.Generic;
using Annium.Finance.Providers.Abstractions.Domain.Market;
using Annium.Finance.Providers.Crypto.Binance.Base.Market.Contracts.Domain;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Market.Contracts.Domain;

/// <summary>
/// The parsed response of the <c>GET /fapi/v1/exchangeInfo</c> endpoint: the account-level rate limits, the
/// margin-eligible assets, and the tradable instruments (symbols).
/// </summary>
/// <param name="RateLimits">The request/order rate limits enforced by the exchange.</param>
/// <param name="Assets">The margin-eligible assets.</param>
/// <param name="Instruments">The tradable instruments (symbols).</param>
internal sealed record ExchangeInfo(
    RateLimits RateLimits,
    IReadOnlyCollection<Asset> Assets,
    IReadOnlyCollection<InstrumentModel> Instruments
);
