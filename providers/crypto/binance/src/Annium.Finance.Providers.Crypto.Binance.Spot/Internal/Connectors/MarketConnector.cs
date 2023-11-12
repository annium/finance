using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Annium.Data.Tables;
using Annium.Finance.Providers.Abstractions.Connectors.Connectors;
using Annium.Finance.Providers.Abstractions.Domain.Dto;
using Annium.Finance.Providers.Abstractions.Domain.Models;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Internal.Connectors;

internal class MarketConnector : IMarketConnector
{
    public event Action<ConnectorStatus> OnStatusChanged = delegate { };
    public ITableView<ResourceDto> Resources { get; }
    public ITableView<InstrumentDto> Instruments { get; }
    public ITableView<InstrumentTicker> Tickers { get; }

    public ValueTask InitAsync()
    {
        throw new NotImplementedException();
    }

    public ValueTask DisposeAsync()
    {
        throw new NotImplementedException();
    }

    public void SubscribeTickers(IReadOnlyCollection<string> symbols)
    {
        throw new NotImplementedException();
    }

    public void UnsubscribeTickers(IReadOnlyCollection<string> symbols)
    {
        throw new NotImplementedException();
    }
}
