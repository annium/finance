using System.Collections.Generic;
using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Domain.Dto;
using Annium.Finance.Providers.Abstractions.Domain.Models;

namespace Annium.Finance.Providers.Abstractions.Connectors.Sync;

public interface IMarketSynchronizer
{
    Task ExecuteAsync(
        MarketSettings config,
        IReadOnlyCollection<ResourceDto> resources,
        IReadOnlyCollection<InstrumentDto> instruments
    );
}
