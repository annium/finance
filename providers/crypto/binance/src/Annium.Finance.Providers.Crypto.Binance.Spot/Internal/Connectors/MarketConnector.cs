using System.Collections.Generic;
using System.Threading.Tasks;
using Annium.Data.Tables;
using Annium.Finance.Providers.Abstractions.Connectors.Connectors;
using Annium.Finance.Providers.Abstractions.Domain.Models;
using Annium.Finance.Providers.Crypto.Binance.Spot.Internal.Services;
using Annium.Finance.Providers.Shared.Connectors;
using Annium.Logging;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Internal.Connectors;

internal class MarketConnector : MarketConnectorBase, IMarketConnector
{
    private readonly BookTickerService _bookTickerService;

    public MarketConnector(
        ITableFactory tableFactory,
        BookTickerService bookTickerService,
        IStatusMonitor monitor,
        ILogger logger
    )
        : base(tableFactory, monitor, logger)
    {
        _bookTickerService = bookTickerService;
        _bookTickerService.OnData += HandleTicker;
        Disposable += () => _bookTickerService.OnData -= HandleTicker;
    }

    public ValueTask InitAsync()
    {
        return ValueTask.CompletedTask;
    }

    public void SubscribeTickers(IReadOnlyCollection<string> symbols)
    {
        _bookTickerService.Subscribe(symbols);
    }

    public void UnsubscribeTickers(IReadOnlyCollection<string> symbols)
    {
        _bookTickerService.Unsubscribe(symbols);
    }

    private void HandleTicker(InstrumentTicker ticker) => TickersTable.Set(ticker);
}
