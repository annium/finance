using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Connectors.Market;
using Annium.Finance.Providers.Abstractions.Connectors.Shared;
using Annium.Finance.Providers.Abstractions.Domain.Market;
using Annium.Finance.Providers.Abstractions.Domain.Shared;
using Annium.Logging;
using Annium.Testing;
using Xunit;

namespace Annium.Finance.Providers.Tests.Lib.Market;

/// <summary>
/// Base for tests that connect to a provider's live market connector and check that the instrument and
/// ticker stream it reports for a fixed symbol come through correctly. Read-only: it never places orders.
/// </summary>
public abstract class MarketConnectorTestBase : ProvidersTestBase
{
    /// <summary>The symbol the derived test drives the market connector scenario for.</summary>
    private readonly string _symbol;

    /// <summary>
    /// Initializes a new instance of the <see cref="MarketConnectorTestBase"/> class.
    /// </summary>
    /// <param name="symbol">The symbol to subscribe to and assert on.</param>
    /// <param name="outputHelper">The xUnit output helper to route trace logging to.</param>
    protected MarketConnectorTestBase(string symbol, ITestOutputHelper outputHelper)
        : base(outputHelper)
    {
        _symbol = symbol;
    }

    /// <summary>
    /// Connects a market connector for the given provider/environment, subscribes to the configured symbol's
    /// ticker and asserts that the instrument metadata and the ticker stream both come through populated.
    /// </summary>
    /// <param name="providerKey">The provider and environment to connect to.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected async Task MarketConnectorBaseAsync(ProviderKey providerKey)
    {
        this.Trace("start");

        // arrange - market components
        this.Trace("get market connector factory");
        var factory = Get<IMarketConnectorFactory>();

        // arrange - create market connector
        var settings = new MarketSettings { Provider = providerKey.Provider, Environment = providerKey.Environment };
        this.Trace("get market connector for {settings}", settings);
        await using var market = factory.Create(settings);

        this.Trace("await market is connected");
        await market.WhenConnectedAsync();

        this.Trace("subscribe to instrument tickers");
        market.SubscribeTickers([_symbol]);

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
        await market.Tickers.FirstAsync(x => x.Symbol == _symbol);
        this.Trace("done");
    }
}
