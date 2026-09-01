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
        var settings = new MarketSettings { Provider = providerKey.Provider };
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

        // the domain guards each of these on `> 0` where it uses them - ToLotSize, the ToTickSize family,
        // and IsValidQtyPrice's two price checks - so a zero means "not enforced" and is a value a correct
        // provider may report. Demanding one, as this fixture used to, would fail it.
        //
        // How a bound goes missing is provider-specific and not what these assertions rest on: a filter
        // absent altogether drops the whole symbol in the converters rather than zeroing a field, and the
        // futures notional filter hard-codes its maximum. What holds regardless is the reading below
        (instrument.MinQty >= 0m).IsTrue($"negative min quantity: {instrument.MinQty}");
        (instrument.LotSize >= 0m).IsTrue($"negative lot size: {instrument.LotSize}");
        (instrument.MinPrice >= 0m).IsTrue($"negative min price: {instrument.MinPrice}");
        (instrument.MaxPrice >= 0m).IsTrue($"negative max price: {instrument.MaxPrice}");
        (instrument.TickSize >= 0m).IsTrue($"negative tick size: {instrument.TickSize}");
        (instrument.MinSum >= 0m).IsTrue($"negative min sum: {instrument.MinSum}");

        // these two are read without that guard - ToValidQty clamps against MaxQty, IsValidQtyPrice compares
        // against MaxSum - so for them zero is not "unbounded" but "nothing is allowed": every quantity
        // clamped to nothing, every order value rejected
        (instrument.MaxQty > 0m).IsTrue("max quantity is zero, which allows no order of any size");
        (instrument.MaxSum > 0m).IsTrue("max sum is zero, which rejects every order value");

        // MaxOrders has no domain consumer at all - nothing in the codebase reads it. This asserts what the
        // exchange means by it rather than what our code does with it, which is the weaker of the two
        // claims and worth marking as such
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

        // and it got there in a good state. Not "no errors reported": a first handshake that drops is an
        // ordinary event on a real network, and the socket answers it by raising OnError and reconnecting
        // within a fraction of a second - so a zero-error assertion would fail a connector that recovered
        // exactly as it should. What must hold is where it ended up
        foreach (var error in errors)
            this.Trace<string>("connector reported an error on the way: {error}", error.Message);

        market.Status.Is(ConnectorStatus.Connected, "the connector delivered a ticker without being connected");

        this.Trace("done");
    }
}
