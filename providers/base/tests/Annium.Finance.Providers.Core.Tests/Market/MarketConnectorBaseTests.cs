using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Connectors.Market;
using Annium.Finance.Providers.Abstractions.Connectors.Shared;
using Annium.Finance.Providers.Abstractions.Domain.Market;
using Annium.Finance.Providers.Abstractions.Domain.Market.Operations;
using Annium.Finance.Providers.Abstractions.Domain.Shared;
using Annium.Finance.Providers.Core.Market;
using Annium.Finance.Providers.Core.Shared.Status;
using Annium.Finance.Providers.Tests.Lib;
using Annium.Logging;
using Annium.Testing;
using Annium.Threading.Tasks;
using NodaTime;
using Xunit;

namespace Annium.Finance.Providers.Core.Tests.Market;

/// <summary>
/// Pins the sync lifecycle of <see cref="MarketConnectorBase"/>: that a sync call runs the connector's own
/// <see cref="MarketConnectorBase.OnSync"/> handler with the resources and instruments it was given, and that
/// tickers written while resubscription is in flight are still delivered to subscribers in order.
/// </summary>
public class MarketConnectorBaseTests : ProvidersTestBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MarketConnectorBaseTests"/> class.
    /// </summary>
    /// <param name="outputHelper">The xUnit output helper used to capture test logs.</param>
    public MarketConnectorBaseTests(ITestOutputHelper outputHelper)
        : base(outputHelper) { }

    /// <summary>
    /// Verifies that triggering a sync on a connector invokes <see cref="MarketConnectorBase.OnSync"/> with the
    /// synced resources and instruments, exposes them via <see cref="MarketConnectorBase.Resources"/> and
    /// <see cref="MarketConnectorBase.Instruments"/>, and still delivers every ticker emitted during the sync to
    /// subscribers, in order.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task Sync_Works()
    {
        // arrange
        this.Trace("prepare settings");
        var settings = new MarketSettings { Provider = "fake", Environment = ProviderEnvironment.Test };

        this.Trace("prepare data");
        const int dataSize = 1000;
        var resources = new[]
        {
            new ResourceModel("BTC", 8),
            new ResourceModel("USDT", 8),
            new ResourceModel("BNB", 8),
        };
        var instruments = new[]
        {
            new InstrumentModel(
                "BTCUSDT",
                resources[0],
                resources[1],
                resources[1],
                0.001m,
                100m,
                0.001m,
                1m,
                100000m,
                0.01m,
                10m,
                1000000m,
                100
            ),
        };

        var tickers = Enumerable
            .Range(0, dataSize)
            .Select(i => new InstrumentTicker(instruments[0].Symbol, i, i))
            .ToArray();

        this.Trace("create connector");
        await using var market = CreateConnector(settings);

        this.Trace("setup sync handler");
        market.OnSync += async (s, res, ins) =>
        {
            s.Is(settings);
            res.SequenceEqual(resources).IsTrue();
            ins.SequenceEqual(instruments).IsTrue();

            this.Trace("sync:start");
            await Task.Delay(200);
            this.Trace("sync:done");
        };

        this.Trace("subscribe to tickers");
        var tickerLog = new TestLog<int>();
        market.Tickers.Subscribe(t => tickerLog.Add((int)t.BidPrice));

        this.Trace("emit tickers");
        Emit(tickers, market.Ticker);

        this.Trace("trigger sync");
        market.Sync(resources, instruments);

        // assert
        this.Trace("await for all events");
        await Wait.UntilAsync(() => tickerLog.Count == dataSize);

        this.Trace("verify tickers log");
        VerifyLog("tickers", tickerLog);
        market.Resources.SequenceEqual(resources).IsTrue();
        market.Instruments.SequenceEqual(instruments).IsTrue();
    }

    /// <summary>
    /// A sync handler that throws is reported, and does not leave the connector claiming to be connected.
    /// The sync cycle unsubscribes the readers before calling the handler and resubscribes after it, so a
    /// handler that throws part-way leaves the connector with no subscriptions at all - and the executor
    /// running it swallows the failure into a log line, which is the last place a caller looks.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task SyncHandlerThrows_IsReportedAndNotClaimedConnected()
    {
        // arrange
        var settings = new MarketSettings { Provider = "fake", Environment = ProviderEnvironment.Test };
        await using var market = CreateConnector(settings);
        var errors = new ConcurrentQueue<ConnectorError>();
        var statuses = new ConcurrentQueue<ConnectorStatus>();
        market.OnError += errors.Enqueue;
        market.OnStatusChanged += statuses.Enqueue;

        market.OnSync += (_, _, _) => throw new InvalidOperationException("sync failed");

        // act
        market.Sync([], []);

        // assert - the failure reaches the caller, and the connector does not call itself connected
        await Expect.ToAsync(() => errors.Count.IsGreaterOrEqual(1));
        statuses.Contains(ConnectorStatus.Connected).IsFalse("a failed sync must not report connected");
    }

    /// <summary>
    /// Builds a <see cref="FakeMarketConnector"/> wired to a fresh <see cref="FakeMarketProvider"/> and this
    /// test's status reporter and monitor.
    /// </summary>
    /// <param name="settings">The market settings to construct the connector with.</param>
    /// <returns>The constructed connector.</returns>
    private FakeMarketConnector CreateConnector(MarketSettings settings)
    {
        var provider = new FakeMarketProvider();
        var reporter = Get<IStatusReporter>();
        var monitor = Get<IStatusMonitor>();

        return new FakeMarketConnector(settings, provider, reporter, monitor, Logger);
    }

    /// <summary>
    /// Schedules a background task that feeds each item in <paramref name="data"/> into <paramref name="emit"/>
    /// with a short delay between items, so tickers arrive while the connector's sync is still in flight.
    /// </summary>
    /// <param name="data">The tickers to emit, in order.</param>
    /// <param name="emit">The callback that pushes a ticker into the connector under test.</param>
    private void Emit(IReadOnlyList<InstrumentTicker> data, Action<InstrumentTicker> emit)
    {
        Task.Run(
                async () =>
                {
                    await Task.Delay(10);
                    foreach (var x in data)
                    {
                        await Task.Delay(1);
                        emit(x);
                    }
                },
                TestContext.Current.CancellationToken
            )
            .GetAwaiter();
    }

    /// <summary>
    /// Asserts that the recorded values in <paramref name="log"/> form a contiguous increasing sequence, proving
    /// none were dropped or reordered; logs the captured entries and rethrows on failure.
    /// </summary>
    /// <param name="type">The label identifying which log is being verified, used in diagnostics.</param>
    /// <param name="log">The log of observed values to verify.</param>
    private void VerifyLog(string type, TestLog<int> log)
    {
        this.Trace<string>("verify {type} log", type);
        var entries = log.ToArray();
        try
        {
            // to entries.Length, not one short of it: stopping early left the final value asserted
            // by nothing. The count is gated separately, so a plain drop was caught - but a last
            // entry that arrived duplicated or out of order passed, and order is what this proves
            for (var i = 1; i < entries.Length; i++)
                entries[i].Is(entries[i - 1] + 1);
        }
        catch
        {
            this.Error<string>("{type} log is not as expected:", type);
            for (var i = 0; i < entries.Length; i++)
                this.Trace("{entry}", entries[i]);
            throw;
        }
    }

    /// <summary>
    /// Exposes <see cref="MarketConnectorBase"/>'s protected sync and ticker-write operations as public methods,
    /// so the test can drive them directly without a real market provider.
    /// </summary>
    private class FakeMarketConnector : MarketConnectorBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FakeMarketConnector"/> class.
        /// </summary>
        /// <param name="settings">The market settings to construct the connector with.</param>
        /// <param name="provider">The market provider backing the connector.</param>
        /// <param name="reporter">The status reporter to bind to.</param>
        /// <param name="monitor">The status monitor to observe.</param>
        /// <param name="logger">The logger to use.</param>
        public FakeMarketConnector(
            MarketSettings settings,
            IMarketProvider provider,
            IStatusReporter reporter,
            IStatusMonitor monitor,
            ILogger logger
        )
            : base(settings, provider, reporter, monitor, Annium.Disposable.AsyncBox(logger), logger) { }

        /// <summary>Triggers a sync with the given resources and instruments, exposing the protected <see cref="MarketConnectorBase.ScheduleSync"/> call.</summary>
        /// <param name="resources">The resources to sync.</param>
        /// <param name="instruments">The instruments to sync.</param>
        public void Sync(IReadOnlyCollection<ResourceModel> resources, IReadOnlyCollection<InstrumentModel> instruments)
        {
            ScheduleSync(resources, instruments);
        }

        /// <summary>Writes a ticker to the connector's output, exposing the protected <see cref="MarketConnectorBase.Write"/> call.</summary>
        /// <param name="ticker">The ticker to write.</param>
        public void Ticker(InstrumentTicker ticker)
        {
            Write(ticker);
        }
    }

    /// <summary>
    /// Stands in for a real <see cref="IMarketProvider"/>; unlike a real provider, both members throw because
    /// the test never calls them - only <see cref="FakeMarketConnector"/>'s exposed sync and ticker operations are exercised.
    /// </summary>
    private class FakeMarketProvider : IMarketProvider
    {
        /// <summary>Not implemented; not exercised by these tests.</summary>
        /// <returns>Never returns.</returns>
        public Task<MarketResult<MarketContext?>> LoadContextAsync()
        {
            throw new NotImplementedException();
        }

        /// <summary>Not implemented; not exercised by these tests.</summary>
        /// <param name="instrument">Unused.</param>
        /// <param name="start">Unused.</param>
        /// <param name="end">Unused.</param>
        /// <param name="ct">Unused.</param>
        /// <returns>Never returns.</returns>
        public IAsyncEnumerable<MarketResult<IReadOnlyCollection<CandleModel>?>> LoadCandlesAsync(
            string instrument,
            Instant start,
            Instant end,
            CancellationToken ct
        )
        {
            throw new NotImplementedException();
        }
    }
}
