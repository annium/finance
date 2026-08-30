using System.Collections.Generic;
using Annium.Finance.Providers.Abstractions.Domain.Market;
using Annium.Finance.Providers.Crypto.Binance.Base.Market.Contracts.Domain;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Internal.Market.Contracts.Domain;

/// <summary>The Binance spot exchange info response: the account/request rate limits and the tradable instruments.</summary>
/// <param name="RateLimits">The rate limits enforced by Binance for this API.</param>
/// <param name="Instruments">The instruments currently open for spot trading.</param>
public sealed record ExchangeInfo(RateLimits RateLimits, IReadOnlyCollection<InstrumentModel> Instruments);
