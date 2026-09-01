using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Connectors.Shared;
using Annium.Finance.Providers.Abstractions.Domain.Market.Operations;
using Annium.Finance.Providers.Core.Internal.Shared.TimeSync;
using Annium.Finance.Providers.Core.Shared.Status;
using Annium.Finance.Providers.Core.Shared.TimeSync;
using Annium.Finance.Providers.Tests.Lib;
using Annium.Testing;
using Xunit;

namespace Annium.Finance.Providers.Core.Tests.Shared.TimeSync;

/// <summary>
/// Pins how the server time source refreshes: what a successful load does to the reported status and to the
/// time it hands out, how the refresh cadence moves between the load and confirm intervals as loads succeed
/// and fail, and what disposal does to a refresh that is still in flight.
/// </summary>
public class ServerTimeSourceTests : ProvidersTestBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ServerTimeSourceTests"/> class.
    /// </summary>
    /// <param name="outputHelper">The xUnit output helper used to capture test logs.</param>
    public ServerTimeSourceTests(ITestOutputHelper outputHelper)
        : base(outputHelper) { }

    /// <summary>
    /// Disposal cancels the refresh before waiting for it. Disposing the timer drains whatever callback is
    /// running, and the cancellation is what lets that callback end; issued afterwards, the wait runs its
    /// whole budget with the load not yet told to stop - and a load outliving that budget then reports
    /// against a reporter disposal has already unbound.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task Dispose_CancelsTheRefreshBeforeWaitingForIt()
    {
        // arrange - a load that ends only when its token says so
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var provider = new GatedServerTimeProvider(started);
        var source = new ServerTimeSource(
            provider,
            new ServerTimeProviderConfig(1, 10_000),
            Get<IStatusReporter>(),
            Logger
        );

        // act
        await started.Task;
        var watch = Stopwatch.StartNew();
        // VSTHRD103: the subject is IDisposable; disposing it synchronously is the behaviour under test
#pragma warning disable VSTHRD103
        source.Dispose();
#pragma warning restore VSTHRD103
        watch.Stop();

        // assert - the drain budget is seconds; cancelled first, this returns in a fraction of it
        (watch.ElapsedMilliseconds < 2000).IsTrue($"disposal took {watch.ElapsedMilliseconds}ms");
        provider.WasCanceled.IsTrue("the in-flight load must be cancelled, not merely waited out");
    }

    /// <summary>
    /// A successful refresh adopts the server's time and reports the source connected. Nothing else tells a
    /// consumer that the time may now be trusted, so a refresh that succeeds without saying so leaves anything
    /// gated on the connector's status waiting on a source that is already working.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task SuccessfulRefresh_AdoptsTheTimeAndReportsConnected()
    {
        // arrange - a server time far from the local clock, so adopting it is unmistakable
        const long serverTime = 1_000_000L;
        var provider = new ScriptedServerTimeProvider(_ => serverTime);
        var monitor = Get<IStatusMonitor>();
        using var source = new ServerTimeSource(
            provider,
            new ServerTimeProviderConfig(1, 10_000),
            Get<IStatusReporter>(),
            Logger
        );

        // act / assert
        await Expect.ToAsync(() => monitor.Status.Is(ConnectorStatus.Connected));
        (source.ServerTime >= serverTime).IsTrue($"server time {source.ServerTime} is before the loaded one");
        (source.ServerTime < serverTime + 60_000).IsTrue(
            $"server time {source.ServerTime} is still the local clock, not the loaded one"
        );
    }

    /// <summary>
    /// Once a refresh succeeds, polling drops from the aggressive load interval to the confirm interval.
    /// Left at the load interval, a synced source keeps hammering the exchange's time endpoint for as long as
    /// it lives, which is how a connection earns a rate-limit ban on it.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task FirstSuccess_SlowsPollingToTheConfirmInterval()
    {
        // arrange - confirm far beyond this test's lifetime. The load interval is spaced well clear of the
        // work it drives, so the switch to confirming lands long before a second load-interval tick could:
        // measured any tighter, a tick queued before the switch takes effect makes the count off by one
        var provider = new ScriptedServerTimeProvider(_ => 1_000_000L);
        using var source = new ServerTimeSource(
            provider,
            new ServerTimeProviderConfig(200, 60_000),
            Get<IStatusReporter>(),
            Logger
        );

        // act - ten load intervals' worth of waiting
        await Expect.ToAsync(() => provider.Calls.IsGreaterOrEqual(1));
        await Task.Delay(2_000, TestContext.Current.CancellationToken);

        // assert - exactly the one refresh that succeeded. Change takes a due time and a period, and both
        // must move to the confirm interval: a due time left on the load interval buys one more refresh
        // before settling, which any bound loose enough to allow a few would miss
        provider.Calls.Is(1, $"the source refreshed {provider.Calls} times after its first success");
    }

    /// <summary>
    /// A confirm refresh that fails puts polling back onto the load interval. Left on the confirm interval,
    /// a source that has lost the exchange rechecks at the cadence chosen for a healthy connection, so it
    /// keeps extrapolating from a stale watch for far longer than intended.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task FailedConfirm_ReturnsPollingToTheLoadInterval()
    {
        // arrange - the first refresh succeeds and switches to confirming; every one after it fails
        var provider = new ScriptedServerTimeProvider(call => call == 1 ? 1_000_000L : null);
        using var source = new ServerTimeSource(
            provider,
            new ServerTimeProviderConfig(10, 250),
            Get<IStatusReporter>(),
            Logger
        );

        // act - the second refresh is the confirm that fails
        await Expect.ToAsync(() => provider.Calls.IsGreaterOrEqual(2));

        // assert - the retry after it comes at once. This is the due time rather than the period, and the
        // two are separate arguments to the same call: left on the confirm interval, the source waits out
        // one more full confirm before its first retry and only then picks up speed
        var watch = Stopwatch.StartNew();
        await Expect.ToAsync(() => provider.Calls.IsGreaterOrEqual(3));
        watch.Stop();
        (watch.ElapsedMilliseconds < 150).IsTrue(
            $"the retry after a failed confirm waited {watch.ElapsedMilliseconds}ms"
        );

        // and the period followed the due time: this many refreshes takes half a second on the 10ms load
        // interval, and longer than this assertion will wait on the 250ms confirm one
        await Expect.ToAsync(() => provider.Calls.IsGreaterOrEqual(50));
    }

    /// <summary>
    /// A server time provider that answers from a script keyed on the refresh number, and counts its calls.
    /// </summary>
    private sealed class ScriptedServerTimeProvider : IServerTimeProvider
    {
        /// <summary>Returns the server time to answer the given 1-based call with, or null to fail it.</summary>
        private readonly Func<int, long?> _script;

        /// <summary>The number of loads requested so far.</summary>
        private int _calls;

        /// <summary>
        /// Initializes a new instance of the <see cref="ScriptedServerTimeProvider"/> class.
        /// </summary>
        /// <param name="script">Returns the server time to answer the given 1-based call with, or null to fail it.</param>
        public ScriptedServerTimeProvider(Func<int, long?> script)
        {
            _script = script;
        }

        /// <summary>Gets the number of loads requested so far.</summary>
        public int Calls => Volatile.Read(ref _calls);

        /// <summary>
        /// Answers the next load from the script.
        /// </summary>
        /// <param name="ct">The cancellation token to observe.</param>
        /// <returns>The scripted server time, or a network failure where the script declines to answer.</returns>
        public Task<MarketResult<long>> LoadAsync(CancellationToken ct)
        {
            var time = _script(Interlocked.Increment(ref _calls));

            return Task.FromResult(
                time is null
                    ? MarketResult.New<long>(MarketOperationStatus.NetworkError, 0)
                    : MarketResult.Ok(time.Value)
            );
        }
    }

    /// <summary>
    /// A server time provider whose load blocks until its token is cancelled.
    /// </summary>
    private sealed class GatedServerTimeProvider : IServerTimeProvider
    {
        /// <summary>Signalled once the first load has begun.</summary>
        private readonly TaskCompletionSource _started;

        /// <summary>
        /// Initializes a new instance of the <see cref="GatedServerTimeProvider"/> class.
        /// </summary>
        /// <param name="started">Signalled once the first load has begun.</param>
        public GatedServerTimeProvider(TaskCompletionSource started)
        {
            _started = started;
        }

        /// <summary>
        /// Gets a value indicating whether the load ended because its token was cancelled.
        /// </summary>
        public bool WasCanceled { get; private set; }

        /// <summary>
        /// Blocks until the given token is cancelled, then reports failure.
        /// </summary>
        /// <param name="ct">The cancellation token to observe.</param>
        /// <returns>A failing result, once cancelled.</returns>
        public async Task<MarketResult<long>> LoadAsync(CancellationToken ct)
        {
            _started.TrySetResult();

            try
            {
                await Task.Delay(Timeout.Infinite, ct);
            }
            catch (OperationCanceledException)
            {
                WasCanceled = true;
            }

            return MarketResult.New<long>(MarketOperationStatus.NetworkError, 0);
        }
    }
}
