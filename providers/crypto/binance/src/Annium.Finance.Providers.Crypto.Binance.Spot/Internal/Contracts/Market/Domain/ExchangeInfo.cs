using System.Collections.Generic;
using Annium.Finance.Providers.Abstractions.Domain.Market;
using Annium.Finance.Providers.Crypto.Binance.Base.Market.Contracts.Domain;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Internal.Contracts.Market.Domain;

public sealed record ExchangeInfo(RateLimits RateLimits, IReadOnlyCollection<InstrumentModel> Instruments);
