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
        // bounded: the overload without a timeout waits on CancellationToken.None, so a cycle that never
        // delivers hangs the run instead of failing it - which is a stuck job rather than a red test
        await Wait.UntilAsync(() => tickerLog.Count == dataSize, 10_000);

        this.Trace("verify tickers log");
        VerifyLog("tickers", tickerLog);
        market.Resources.SequenceEqual(resources).IsTrue();
        market.Instruments.SequenceEqual(instruments).IsTrue();

        // and the cycle ends by saying so. Its failing counterpart asserts the connector must not claim to be
        // connected when the handler throws; nothing asserted that it does when the handler returns, so a
        // cycle that completed and left the connector reading as still connecting looked correct
        market.Status.Is(ConnectorStatus.Connected, "a completed sync leaves the connector connected");
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
    /// An error a component reports through its status reporter reaches the connector's own listeners. This
    /// is the far half of a relay the campaign already repaired at its near end: the monitor was raising
    /// nothing at all, and now that it does, the connector still has to pass it on. The other route into
    /// <c>OnError</c> — a sync handler that throws — is tested above and does not touch this one.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ErrorReportedByAnotherComponent_ReachesTheConnector()
    {
        // arrange - a second component bound to the same monitor, as a provider's loaders are
        var other = Get<IStatusReporter>();
        other.Bind("other", ConnectorStatus.Connected);

        var settings = new MarketSettings { Provider = "fake", Environment = ProviderEnvironment.Test };
        await using var market = CreateConnector(settings);
        var errors = new ConcurrentQueue<ConnectorError>();
        market.OnError += errors.Enqueue;

        // act
        other.Error(new ConnectorError("websocket dropped"));

        // assert
        await Expect.ToAsync(() => errors.Count.Is(1));
        errors.TryPeek(out var error).IsTrue();
        error.NotNull().Message.Is("websocket dropped", "the error must arrive intact, not merely as a signal");
    }

    /// <summary>
    /// A connector disposed while a sync cycle is still running leaves nothing flowing behind it. The cycle
    /// disposes and <em>resets</em> the box holding its subscriptions on every pass, and a reset clears that
    /// box's disposed flag — so while the executor and that box were unordered siblings in one disposable
    /// box, a cycle finishing during the drain could revive a box the teardown had already passed over and
    /// fill it with subscriptions nothing would dispose again.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task DisposalDuringSync_LeavesNothingSubscribed()
    {
        // arrange - a sync cycle held open until this test lets it finish
        var settings = new MarketSettings { Provider = "fake", Environment = ProviderEnvironment.Test };
        var market = CreateConnector(settings);
        var tickers = new ConcurrentQueue<InstrumentTicker>();
        market.Tickers.Subscribe(tickers.Enqueue);

        var syncing = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        market.OnSync += async (_, _, _) =>
        {
            syncing.TrySetResult();
#pragma warning disable VSTHRD003
            await release.Task;
#pragma warning restore VSTHRD003
        };

        market.Sync([], []);
        await syncing.Task;

        // act - tear down with the cycle still in flight, then let it run to its end
        var disposal = market.DisposeAsync();
        release.TrySetResult();
        await disposal;

        // assert - whatever the cycle did on its way out, the connector forwards nothing now
        tickers.Clear();
        market.Ticker(new InstrumentTicker("BTCUSDT", 1m, 1m));
        await Task.Delay(100, TestContext.Current.CancellationToken);
        tickers.IsEmpty("a disposed connector must not still be piping tickers to its subscribers");
    }

    /// <summary>
    /// A disposed connector stops counting as one of its monitor's targets. Binding registers it, nothing
    /// else removes it, and disposal reports no status — so left registered it sits there at whatever status
    /// it last held, and the monitor keeps resolving an overall status from a component that no longer
    /// exists. Each factory hands a connector its own scope today, which is what keeps this out of the way;
    /// the contract should not depend on that.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task DisposedConnector_StopsCountingTowardsItsMonitor()
    {
        // arrange
        var monitor = Get<IStatusMonitor>();
        var settings = new MarketSettings { Provider = "fake", Environment = ProviderEnvironment.Test };
        var market = CreateConnector(settings);
        monitor.Status.Is(ConnectorStatus.Connected, "the connector registers itself as a connected target");

        // act
        await market.DisposeAsync();

        // assert - no targets left at all, which is what an empty monitor resolves to
        monitor.Status.Is(
            ConnectorStatus.Disconnected,
            "a disposed connector must unregister, not linger at its last status"
        );
    }

    /// <summary>
    /// A connector stops listening to its monitor before it stops counting towards it. Unregistering a target
    /// recomputes the aggregate status, which can raise the monitor's event synchronously — so unbinding while
    /// still subscribed delivers a status change to a connector already being torn down. On a transition to
    /// connected that lands in <c>HandleSync</c>, scheduling a fresh resync on an executor the disposal has not
    /// reached yet, against resources it is about to release.
    /// </summary>
    /// <remarks>
    /// The sibling test above registers the connector as the monitor's only target, and with one target
    /// unregistering always resolves to disconnected — the path where the aggregate changes into something the
    /// connector acts on is unreachable from that setup. This one keeps a second component bound so removing
    /// the connector's own target actually moves the aggregate.
    /// </remarks>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task DisposingConnector_StopsListeningBeforeItUnbinds()
    {
        // arrange - a second component on the same monitor, sitting at disconnected
        var monitor = Get<IStatusMonitor>();
        var other = Get<IStatusReporter>();
        other.Bind("other", ConnectorStatus.Disconnected);

        var settings = new MarketSettings { Provider = "fake", Environment = ProviderEnvironment.Test };
        var market = CreateConnector(settings);
        monitor.Status.Is(
            ConnectorStatus.Connecting,
            "one connected and one disconnected target resolve to connecting"
        );

        var statuses = new ConcurrentQueue<ConnectorStatus>();
        market.OnStatusChanged += statuses.Enqueue;

        // act - disposal removes this connector's target, leaving only the disconnected one
        await market.DisposeAsync();

        // assert - the aggregate did move, and none of it reached the connector
        monitor.Status.Is(ConnectorStatus.Disconnected);
        statuses.IsEmpty("a connector being disposed must not still be handling its monitor's events");
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
