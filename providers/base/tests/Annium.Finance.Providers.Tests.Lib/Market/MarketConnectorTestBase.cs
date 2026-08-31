using System.Collections.Concurrent;
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

        // the connector reports its faults through OnError and nothing else - a resync that failed, a
        // reconnect that dropped. Its user counterpart has collected these from the start; this half never
        // did, so a run that recovered in time to deliver a ticker passed with the fault unmentioned
        var errors = new ConcurrentQueue<ConnectorError>();
        market.OnError += errors.Enqueue;

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

        // a bound the exchange does not enforce arrives as zero, and the domain reads it that way - every
        // one of these is guarded on `> 0` where it is used. Demanding a value would fail a correct
        // provider on any symbol the exchange leaves unbounded, which is what this fixture used to do
        (instrument.MinQty >= 0m).IsTrue($"negative min quantity: {instrument.MinQty}");
        (instrument.LotSize >= 0m).IsTrue($"negative lot size: {instrument.LotSize}");
        (instrument.MinPrice >= 0m).IsTrue($"negative min price: {instrument.MinPrice}");
        (instrument.MaxPrice >= 0m).IsTrue($"negative max price: {instrument.MaxPrice}");
        (instrument.TickSize >= 0m).IsTrue($"negative tick size: {instrument.TickSize}");
        (instrument.MinSum >= 0m).IsTrue($"negative min sum: {instrument.MinSum}");

        // these three are read without that guard, so zero is not "unbounded" but "nothing is allowed": a
        // zero max clamps every quantity to nothing, rejects every sum, and permits no orders at all
        (instrument.MaxQty > 0m).IsTrue("max quantity is zero, which allows no order of any size");
        (instrument.MaxSum > 0m).IsTrue("max sum is zero, which rejects every order value");
        (instrument.MaxOrders > 0).IsTrue("max orders is zero, which permits no orders at all");

        // and a bound that is set must not be inverted - the pair being present says nothing about it
        (instrument.MaxQty >= instrument.MinQty).IsTrue(
            $"max quantity {instrument.MaxQty} is below min {instrument.MinQty}"
        );
        (instrument.MaxSum >= instrument.MinSum).IsTrue(
            $"max sum {instrument.MaxSum} is below min {instrument.MinSum}"
        );
        if (instrument.MaxPrice > 0m)
            (instrument.MaxPrice >= instrument.MinPrice).IsTrue(
                $"max price {instrument.MaxPrice} is below min {instrument.MinPrice}"
            );

        // assert - tickers
        this.Trace("ensure tickers are loaded");
        await market.Tickers.FirstAsync(x => x.Symbol == _symbol);

        // and nothing went wrong on the way there
        errors.Count.Is(0, $"the connector reported errors: {string.Join("; ", errors.Select(x => x.Message))}");

        this.Trace("done");
    }
}
