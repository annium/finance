using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Domain.Market;
using Annium.Finance.Providers.Abstractions.Domain.Market.Operations;
using Annium.Finance.Providers.Abstractions.Domain.Shared;
using NodaTime;

namespace Annium.Finance.Providers.Abstractions.Connectors.Market;

public interface IMarketProvider
{
    Task<MarketResult<MarketContext?>> LoadContextAsync(ProviderEnvironment env);

    IAsyncEnumerable<MarketResult<IReadOnlyCollection<CandleModel>?>> LoadCandlesAsync(
        string instrument,
        ProviderEnvironment env,
        Instant start,
        Instant end,
        CancellationToken ct
    );
}
