using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Annium.Extensions.Pooling;
using Annium.Finance.Providers.Abstractions.Connectors.Connectors;
using Annium.Finance.Providers.Abstractions.Domain.Models;
using Annium.Finance.Providers.Shared;
using Annium.Logging;
using Annium.Testing;
using Xunit.Abstractions;

namespace Annium.Finance.Providers.Tests.Shared.Connectors;

public abstract class MarketConnectorTestBase : ConnectorTestBase
{
    private readonly string _symbol;

    protected MarketConnectorTestBase(
        Action<ProviderRegistrationContext> registerProvider,
        string symbol,
        ITestOutputHelper outputHelper
    )
        : base(registerProvider, outputHelper)
    {
        _symbol = symbol;
    }

    protected async Task MarketConnectorBaseAsync(ProviderKey providerKey)
    {
        this.Trace("start");

        // arrange - market components
        this.Trace("get market connectors cache");
        var marketCache = Get<IObjectCache<MarketSettings, IMarketConnector>>();

        // arrange - resolve market ref
        var marketConfig = new MarketSettings(providerKey.Provider, providerKey.Environment);
        this.Trace("get market connector for {config}", marketConfig);
        await using var marketRef = await marketCache.GetAsync(marketConfig);
        var market = marketRef.Value;

        this.Trace("await market is connected");
        await market.WhenConnected();

        this.Trace("subscribe to instrument tickers");
        market.SubscribeTickers(new[] { _symbol });

        // assert - instruments
        market.Instruments.Count.IsGreater(0);
        this.Trace<string>("resolve instrument for symbol {symbol}", _symbol);
        var instrument = market.Instruments.Single(x => x.Symbol == _symbol);
        instrument.Target.IsNotDefault();
        instrument.Target.Code.IsNullOrWhiteSpace().IsFalse();
        market.Resources.Contains(instrument.Target).IsTrue();
        instrument.Quote.IsNotDefault();
        instrument.Quote.Code.IsNullOrWhiteSpace().IsFalse();
        market.Resources.Contains(instrument.Quote).IsTrue();
        instrument.Currency.IsNotDefault();
        instrument.Currency.Code.IsNullOrWhiteSpace().IsFalse();
        market.Resources.Contains(instrument.Currency).IsTrue();
        instrument.Symbol.IsNullOrWhiteSpace().IsFalse();
        instrument.MinQty.IsNotDefault();
        instrument.MaxQty.IsNotDefault();
        instrument.LotSize.IsNotDefault();
        instrument.MinPrice.IsNotDefault();
        instrument.MaxPrice.IsNotDefault();
        instrument.TickSize.IsNotDefault();
        instrument.MinSum.IsNotDefault();
        instrument.MaxSum.IsNotDefault();
        instrument.MaxOrders.IsNotDefault();

        // assert - tickers
        this.Trace("ensure tickers are loaded");
        var ticker = await market.Tickers.FirstAsync(x => x.Symbol == _symbol);
        this.Trace("done");
    }
}
