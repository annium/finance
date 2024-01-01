using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Domain.Enums;
using Annium.Finance.Providers.Abstractions.Domain.Models;
using Annium.Finance.Providers.Abstractions.Domain.Operations;
using NodaTime;

namespace Annium.Finance.Providers.Abstractions.Connectors.Connectors;

public interface IMarketProvider
{
    Task<MarketResult<MarketContext>> LoadContextAsync(ProviderEnvironment env);

    IAsyncEnumerable<MarketResult<IReadOnlyCollection<CandleModel>>> LoadCandlesAsync(
        string instrument,
        ProviderEnvironment env,
        Instant start,
        Instant end,
        CancellationToken ct
    );
}
