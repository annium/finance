using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Domain.Dto;
using Annium.Finance.Providers.Abstractions.Domain.Enums;
using Annium.Finance.Providers.Abstractions.Domain.Operations;
using NodaTime;

namespace Annium.Finance.Providers.Abstractions.Connectors.Connectors;

public interface IMarketProvider
{
    Task<MarketResult<MarketContext>> LoadContextAsync(ProviderEnvironment env);

    IAsyncEnumerable<MarketResult<IReadOnlyCollection<CandleDto>>> LoadCandlesAsync(
        string instrument,
        ProviderEnvironment env,
        Instant start,
        Instant end,
        CancellationToken ct
    );
}

public sealed record MarketContext(
    IReadOnlyCollection<ResourceDto> Resources,
    IReadOnlyCollection<InstrumentDto> Instruments
);
