using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Domain.Dto;
using Annium.Finance.Providers.Abstractions.Domain.Models;

namespace Annium.Finance.Providers.Abstractions.Connectors.Connectors;

public interface IMarketConnector : IConnectorBase
{
    IReadOnlyCollection<ResourceDto> Resources { get; }
    IReadOnlyCollection<InstrumentDto> Instruments { get; }
    IObservable<InstrumentTicker> Tickers { get; }
    event Func<MarketSettings, IReadOnlyCollection<ResourceDto>, IReadOnlyCollection<InstrumentDto>, Task> OnSync;
    void SubscribeTickers(IReadOnlyCollection<string> symbols);
    void UnsubscribeTickers(IReadOnlyCollection<string> symbols);
}
