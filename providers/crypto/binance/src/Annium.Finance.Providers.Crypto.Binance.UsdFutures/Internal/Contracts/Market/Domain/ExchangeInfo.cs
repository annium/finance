using System.Collections.Generic;
using Annium.Finance.Providers.Abstractions.Domain.Market;
using Annium.Finance.Providers.Crypto.Binance.Base.Market.Contracts.Domain;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Contracts.Market.Domain;

internal sealed record ExchangeInfo(
    RateLimits RateLimits,
    IReadOnlyCollection<Asset> Assets,
    IReadOnlyCollection<InstrumentModel> Instruments
);
