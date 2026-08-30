using System.Collections.Concurrent;
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
        _statuses.IsEqual(new[] { Connecting, Connected });
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
