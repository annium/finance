using System;
using Annium.Data.Tables;
using Annium.Finance.Providers.Abstractions.Domain.Dto;
using Annium.Finance.Providers.Abstractions.Domain.Interfaces;
using Annium.Finance.Providers.Abstractions.Domain.Models;

namespace Annium.Finance.Providers.Abstractions.Connectors.Connectors;

public interface IMarketConnector : IConnectorBase<IMarketConfig>, IAsyncDisposable
{
    ITableView<ResourceDto> Resources { get; }
    ITableView<InstrumentDto> Instruments { get; }
    ITableView<InstrumentTicker> Tickers { get; }
}
