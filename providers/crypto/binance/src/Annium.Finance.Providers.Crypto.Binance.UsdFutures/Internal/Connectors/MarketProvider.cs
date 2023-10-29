using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Connectors.Connectors;
using Annium.Finance.Providers.Abstractions.Domain.Dto;
using Annium.Finance.Providers.Abstractions.Domain.Enums;
using Annium.Finance.Providers.Abstractions.Domain.Operations;
using NodaTime;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Connectors;

internal class MarketProvider : IMarketProvider
{
    public Task<MarketResult<MarketContext>> LoadContextAsync(ProviderEnvironment env)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<MarketResult<IReadOnlyCollection<CandleDto>>> LoadCandlesAsync(
        string instrument,
        ProviderEnvironment env,
        Instant start,
        Instant end,
        CancellationToken ct
    )
    {
        throw new NotImplementedException();
    }
}
