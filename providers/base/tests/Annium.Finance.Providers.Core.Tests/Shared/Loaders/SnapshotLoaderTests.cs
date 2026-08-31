using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Connectors.Shared;
using Annium.Finance.Providers.Abstractions.Domain.Market.Operations;
using Annium.Finance.Providers.Abstractions.Domain.Shared.Operations;
using Annium.Finance.Providers.Core.Shared;
using Annium.Finance.Providers.Core.Shared.Loaders;
using Annium.Finance.Providers.Core.Shared.Status;
using Annium.Testing;
using Xunit;
using static Annium.Finance.Providers.Abstractions.Connectors.Shared.ConnectorStatus;

namespace Annium.Finance.Providers.Core.Tests.Shared.Loaders;

/// <summary>
/// Pins the retry and cancellation behavior of <see cref="ISnapshotLoader{T}"/>: that it keeps retrying a failing
/// fetch until one succeeds, that stopping it while a fetch is in flight discards that fetch's result, and that
/// it can run without reporting a connecting status.
/// </summary>
public class SnapshotLoaderTests : TestBase
{
    /// <summary>Records every connection status transition reported by the loader's status monitor, in order.</summary>
    private readonly ConcurrentQueue<ConnectorStatus> _statuses = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="SnapshotLoaderTests"/> class, registering the finance
    /// providers services and test log used to observe loaded data.
    /// </summary>
    /// <param name="outputHelper">The xUnit output helper used to capture test logs.</param>
    public SnapshotLoaderTests(ITestOutputHelper outputHelper)
        : base(outputHelper)
    {
        Register(container =>
        {
            container.AddFinanceProviders();
        });
        this.RegisterTestLogs();
    }

    /// <summary>
    /// The provider is built by the base class during initialization, so anything resolved from it has to
    /// wait for that - a constructor runs too early.
    /// </summary>
    /// <returns>A task representing the asynchronous initialization.</returns>
    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();

        var monitor = Get<IStatusMonitor>();
        monitor.OnStatusChanged += _statuses.Enqueue;
    }

    /// <summary>
    /// Verifies that a loader whose fetch delegate fails repeatedly keeps retrying, on its own, until the fetch
    /// eventually succeeds, and reports connecting then connected on the status monitor.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task Works()
    {
        var cfg = new SnapshotLoaderConfig(1, 2, 5);
        var attempt = 0;
        var log = Get<TestLog<int>>();
        async Task<MarketResult<int>> Load()
        {
            attempt++;

            await Task.Delay(5, CancellationToken.None);

            return attempt < 10
                ? MarketResult.New(MarketOperationStatus.NotFound, 0, $"No data at {attempt}")
                : MarketResult.Ok(attempt++);
        }
        using var loader = Provider.CreateSnapshotLoader<int>(cfg, async _ => await Load());
        loader.OnData += log.Add;

        loader.Start(true);

        await Expect.ToAsync(() => log.Has(1));
        log.At(0).Is(10);

        // wait on the statuses before asserting them. The successful fetch hands the data to OnData a line
        // before it reports connected, so a poll on the log alone can pass while the status has not been
        // reported yet - the same mistake as three other tests in this suite once had
        await Expect.ToAsync(() => _statuses.Count.IsGreaterOrEqual(2));
        _statuses.IsEqual(new[] { Connecting, Connected });
    }

    /// <summary>
    /// The loader backs off to the slow interval once it has made as many failed attempts as its fast-request
    /// limit allows — on that attempt, not one after it. Every extra attempt at the fast interval is a request
    /// against the exchange's rate limit that the configuration said not to make.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task FastRequestLimitReached_SwitchesToTheSlowInterval()
    {
        // arrange - fast retries every 1ms, slow every 2s, backing off after 2 failed attempts
        var cfg = new SnapshotLoaderConfig(1, 2, 2000);
        var attempts = 0;
        using var loader = Provider.CreateSnapshotLoader<int>(
            cfg,
            async _ =>
            {
                Interlocked.Increment(ref attempts);

                // each attempt takes long enough that the timer cannot queue the next one while the switch
                // to the slow interval is still being applied. With an instant fetch a third tick could
                // already be due before the change took effect, and the count was not deterministic
                await Task.Delay(40, CancellationToken.None);

                return MarketResult.New(MarketOperationStatus.NotFound, 0, "no");
            }
        );

        // act - let it reach the limit, then wait far longer than the fast interval and far less than the slow
        loader.Start(true);
        await Expect.ToAsync(() => Volatile.Read(ref attempts).IsGreaterOrEqual(2));
        await Task.Delay(300, TestContext.Current.CancellationToken);

        // assert - exactly the two the limit allows. The switch turns on a counter rather than a clock, so
        // once it has fired nothing more is due for two seconds and the count is settled; a boundary off by
        // one shows up here as exactly one extra attempt, which a looser bound would let through
        Volatile
            .Read(ref attempts)
            .Is(2, "the loader must back off on the attempt that reaches its limit, not the one after it");
    }

    /// <summary>
    /// Disposal does not wait on a callback that is waiting on disposal. Disposing the timer drains whatever
    /// callback is running, and that callback takes the loader's lock — so draining while holding that lock
    /// leaves each waiting for the other until the drain budget runs out, several seconds later, and leaks
    /// the wait handle it gave up on. The same shape the rate limiter's disposal was already written around.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task Dispose_DoesNotBlockOnItsOwnCallback()
    {
        // arrange - retry every millisecond with a fetch slow enough that one is always in flight
        var cfg = new SnapshotLoaderConfig(1, 100, 1);
        var attempts = 0;
        var loader = Provider.CreateSnapshotLoader<int>(
            cfg,
            async _ =>
            {
                Interlocked.Increment(ref attempts);
                await Task.Delay(30, CancellationToken.None);

                return MarketResult.New(MarketOperationStatus.NotFound, 0, "no");
            }
        );

        loader.Start(true);
        await Expect.ToAsync(() => Volatile.Read(ref attempts).IsGreaterOrEqual(2));

        // act
        var watch = Stopwatch.StartNew();
        await loader.DisposeAsync();
        watch.Stop();

        // assert - the drain budget is seconds; the callback and the disposal not blocking each other
        // means this returns in a fraction of it
        (watch.ElapsedMilliseconds < 2000).IsTrue($"disposal took {watch.ElapsedMilliseconds}ms");
    }

    /// <summary>
    /// Verifies that calling <see cref="ISnapshotLoader{T}.Stop"/> while a fetch is in flight discards that
    /// fetch's result once it later completes: no data is delivered, and the status monitor reports connecting
    /// then disconnected rather than connected.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task StopsDuringFetch_CancelsProcessing()
    {
        var cfg = new SnapshotLoaderConfig(1, 2, 5);
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var gate = new TaskCompletionSource<MarketResult<int>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var log = Get<TestLog<int>>();
        var loader = Provider.CreateSnapshotLoader<int>(
            cfg,
            async _ =>
            {
                started.TrySetResult();
#pragma warning disable VSTHRD003
                return await gate.Task;
#pragma warning restore VSTHRD003
            }
        );
        loader.OnData += log.Add;

        loader.Start(true);

        await started.Task;
        loader.Stop();
        gate.TrySetResult(MarketResult.Ok(1));

        await Task.Delay(30, CancellationToken.None);

        log.Count.Is(0);

        await loader.DisposeAsync();
        await Expect.ToAsync(() => _statuses.Count.IsGreaterOrEqual(2));
        _statuses.ToArray().IsEqual(new[] { Connecting, Disconnected });
    }

    /// <summary>
    /// A fetch left over from a stopped cycle does not speak for the cycle that replaced it. Stopping and
    /// starting again gives the loader a fresh cancellation source, and the old fetch - issued under the
    /// previous one - must be discarded when it finally returns, not delivered as though it answered the
    /// new cycle's question.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task StaleFetch_FromAStoppedCycle_IsDiscarded()
    {
        // arrange - the first fetch is held open; the second answers at once, with a different value
        var cfg = new SnapshotLoaderConfig(1, 2, 5);
        var first = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var gate = new TaskCompletionSource<MarketResult<int>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = 0;
        var log = Get<TestLog<int>>();
        var loader = Provider.CreateSnapshotLoader<int>(
            cfg,
            async _ =>
            {
                if (Interlocked.Increment(ref calls) == 1)
                {
                    first.TrySetResult();
#pragma warning disable VSTHRD003
                    return await gate.Task;
#pragma warning restore VSTHRD003
                }

                return MarketResult.Ok(999);
            }
        );
        loader.OnData += log.Add;

        // act - stop while the first fetch is in flight, start again, then let the stale one return
        loader.Start(true);
        await first.Task;
        loader.Stop();
        loader.Start(true);

        // the stale answer lands after the restart. The timer is sequential, so the second cycle cannot
        // begin until this call returns - which is exactly why the stale result must not be acted upon
        gate.TrySetResult(MarketResult.Ok(1));
        await Expect.ToAsync(() => log.Count.IsGreaterOrEqual(1));

        // assert
        log.IsEqual(new[] { 999 });

        await loader.DisposeAsync();
    }

    /// <summary>
    /// A loader that has been disposed stops counting towards the connector's status. It reports itself
    /// disconnected on the way out, which is a transition worth seeing - but if it also stays registered,
    /// the monitor holds a disconnected target next to the live ones forever, and the connector can never
    /// report itself connected again for as long as it lives.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task DisposedLoader_StopsHoldingTheStatusDown()
    {
        // arrange - a second target that stays connected, so the monitor has something to be connected about
        var monitor = Get<IStatusMonitor>();
        var survivor = Get<IStatusReporter>();
        survivor.Bind(this);
        survivor.Connected();

        var loader = Provider.CreateSnapshotLoader<int>(
            new SnapshotLoaderConfig(1, 2, 5),
            _ => Task.FromResult<IBaseResult<int>>(MarketResult.Ok(1))
        );
        loader.Start(true);
        await Expect.ToAsync(() => monitor.Status.Is(Connected));

        // act
        await loader.DisposeAsync();

        // assert
        await Expect.ToAsync(() => monitor.Status.Is(Connected));
    }

    /// <summary>
    /// Verifies that starting a loader with <c>reportStatus: false</c> still delivers data on a successful fetch,
    /// while the status monitor jumps straight to connected without ever reporting connecting.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task StartsWithoutStatusReporting()
    {
        var cfg = new SnapshotLoaderConfig(1, 2, 5);
        var log = Get<TestLog<int>>();
        var loader = Provider.CreateSnapshotLoader(cfg, _ => Task.FromResult<IBaseResult<int>>(MarketResult.Ok(7)));
        loader.OnData += log.Add;

        loader.Start(false);

        await Expect.ToAsync(() => log.Has(1));
        log.At(0).Is(7);

        await loader.DisposeAsync();
        await Expect.ToAsync(() => _statuses.Count.IsGreaterOrEqual(2));
        _statuses.ToArray().IsEqual(new[] { Connected, Disconnected });
    }
}
