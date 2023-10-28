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
    Task<
        Result<MarketOperationStatus, (IReadOnlyCollection<ResourceDto>, IReadOnlyCollection<InstrumentDto>)>
    > LoadResourcesAndInstrumentsAsync(ProviderEnvironment env);

    IAsyncEnumerable<Result<MarketOperationStatus, IReadOnlyCollection<CandleDto>>> LoadCandlesAsync(
        string instrument,
        ProviderEnvironment env,
        Instant start,
        Instant end,
        CancellationToken ct
    );
}
