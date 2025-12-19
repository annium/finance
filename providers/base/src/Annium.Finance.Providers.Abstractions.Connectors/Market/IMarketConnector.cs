using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Connectors.Shared;
using Annium.Finance.Providers.Abstractions.Domain.Market;

namespace Annium.Finance.Providers.Abstractions.Connectors.Market;

public interface IMarketConnector : IConnectorBase
{
    IReadOnlyCollection<ResourceModel> Resources { get; }
    IReadOnlyCollection<InstrumentModel> Instruments { get; }
    IObservable<InstrumentTicker> Tickers { get; }
    event Func<MarketSettings, IReadOnlyCollection<ResourceModel>, IReadOnlyCollection<InstrumentModel>, Task> OnSync;
    void SubscribeTickers(IReadOnlyCollection<string> symbols);
    void UnsubscribeTickers(IReadOnlyCollection<string> symbols);
}
