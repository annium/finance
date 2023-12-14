using System.Collections.Generic;
using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Connectors.Sync;
using Annium.Finance.Providers.Abstractions.Domain.Dto;
using Annium.Finance.Providers.Abstractions.Domain.Models;
using Annium.Logging;

namespace Annium.Finance.Providers.Shared.Internal.Sync;

internal class NoopMarketSynchronizer : IMarketSynchronizer, ILogSubject
{
    public ILogger Logger { get; }

    public NoopMarketSynchronizer(ILogger logger)
    {
        Logger = logger;
    }

    public Task ExecuteAsync(
        MarketSettings config,
        IReadOnlyCollection<ResourceDto> resources,
        IReadOnlyCollection<InstrumentDto> instruments
    )
    {
        this.Trace("run");

        return Task.CompletedTask;
    }
}
