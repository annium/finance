using System.Collections.Generic;
using Annium.Data.Tables;
using Annium.Finance.Providers.Abstractions.Domain.Dto;
using Annium.Finance.Providers.Abstractions.Domain.Models;

namespace Annium.Finance.Providers.Abstractions.Connectors.Connectors;

public interface IMarketConnector : IConnectorBase
{
    ITableView<ResourceDto> Resources { get; }
    ITableView<InstrumentDto> Instruments { get; }
    ITableView<InstrumentTicker> Tickers { get; }
    void SubscribeTickers(IReadOnlyCollection<string> symbols);
    void UnsubscribeTickers(IReadOnlyCollection<string> symbols);
}
