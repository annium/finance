using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Connectors.Shared;
using Annium.Finance.Providers.Abstractions.Domain.Market.Operations;
using Annium.Finance.Providers.Abstractions.Domain.Shared.Operations;
using Annium.Finance.Providers.Core.Shared;
using Annium.Finance.Providers.Core.Shared.Loaders;
using Annium.Finance.Providers.Core.Shared.Status;
using Annium.Linq;
using Annium.Logging;
using Annium.Testing;
using Xunit;
using static Annium.Finance.Providers.Abstractions.Connectors.Shared.ConnectorStatus;

namespace Annium.Finance.Providers.Core.Tests.Shared.Loaders;

/// <summary>
/// Pins the reload-triggering and lifecycle behavior of <see cref="ICompositeLoader{T}"/>: that interval timers,
/// debounced requests and manual requests all eventually produce data, that a request is a no-op before the
/// loader starts, and that stopping it blocks further requests from reaching the fetch delegate.
/// </summary>
public class CompositeLoaderTests : TestBase
{
    /// <summary>Records every connection status transition reported by the loader's status monitor, in order.</summary>
    private readonly ConcurrentQueue<ConnectorStatus> _statuses = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="CompositeLoaderTests"/> class, registering the finance
    /// providers services and test log used to observe loaded data.
    /// </summary>
    /// <param name="outputHelper">The xUnit output helper used to capture test logs.</param>
    public CompositeLoaderTests(ITestOutputHelper outputHelper)
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
    /// Verifies that a loader eventually delivers data regardless of which reload trigger is exercised: a
    /// scheduled interval reload, a burst of debounced <see cref="ICompositeLoader{T}.Request"/> calls, or both
    /// disabled (relying only on the underlying snapshot loader's own retries) - and that the status monitor
    /// reports connecting then connected.
    /// </summary>
    /// <param name="interval">The interval, in milliseconds, between scheduled reloads (zero disables it).</param>
    /// <param name="debounce">The debounce period, in milliseconds, for <see cref="ICompositeLoader{T}.Request"/> (zero disables it).</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Theory]
    [InlineData(200, 3000)]
    [InlineData(200, 0)]
    [InlineData(3000, 50)]
    [InlineData(0, 50)]
    public async Task Works(int interval, int debounce)
    {
        var cfg = new CompositeLoaderConfig(1, 5, 2, interval, debounce);
        var attempt = 0;
        var log = Get<TestLog<int>>();
        async Task<MarketResult<int>> Load()
        {
            attempt++;

            await Task.Delay(5, CancellationToken.None);

            return attempt != 3
                ? MarketResult.New(MarketOperationStatus.NotFound, 0, $"No data at {attempt}")
                : MarketResult.Ok(attempt++);
        }
        using var loader = Provider.CreateCompositeLoader<int>(cfg, async _ => await Load());
        loader.OnData += log.Add;

        loader.Start(true);
        for (var i = 0; i < 10; i++)
            loader.Request();

        await Expect.ToAsync(() => log.Has(1));
        log.At(0).Is(3);

        // the opening of the sequence, not the whole of it. Snapshotting the statuses at the top of the
        // predicate read them before the condition that follows had held, so the run where the data
        // finally arrived could still be looking at a queue from before it connected. And the tail is not
        // fixed either: with a reload interval set, the next scheduled load fails again and appends
        // another Connecting. What this test is about is that the loader connects, having been connecting
        await Expect.ToAsync(() => _statuses.Count.IsGreaterOrEqual(2));
        var statuses = _statuses.Take(2).ToArray();
        this.Trace<string>("statuses: {statuses}", statuses.Select(x => x.ToString()).Join(", "));
        statuses.IsEqual(new[] { Connecting, Connected });
    }

    /// <summary>
    /// Verifies that calling <see cref="ICompositeLoader{T}.Request"/> before <see cref="ICompositeLoader{T}.Start"/>
    /// has no effect: the fetch delegate is never invoked and no status is reported.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task RequestIsIgnoredWhenInactive()
    {
        var cfg = new CompositeLoaderConfig(1, 1, 2, 50, 10);
        var attempts = 0;
        var loader = Provider.CreateCompositeLoader(
            cfg,
            _ =>
            {
                attempts++;
                return Task.FromResult<IBaseResult<int>>(MarketResult.Ok(attempts));
            }
        );

        loader.Request();

        await Task.Delay(30, CancellationToken.None);
        attempts.Is(0);

        await loader.DisposeAsync();
        _statuses.IsEmpty.IsTrue();
    }

    /// <summary>
    /// A stopped loader makes no further requests, however its interval is configured. Every other test here
    /// either enables the interval and never stops, or stops with the interval disabled, so the two together
    /// went unexercised.
    /// </summary>
    /// <remarks>
    /// On the interval path two things enforce this and either alone is enough: <see cref="ICompositeLoader{T}.Stop"/>
    /// disarms the interval timer, and the callback declines when the loader is not active. So on this path
    /// neither is pinned on its own — removing one leaves the other doing the work and this test still passes;
    /// removing both does fail it, which is what it pins. The debounce path is not like that at all: see
    /// <see cref="StoppedLoader_IsNotRestartedByAPendingDebounce"/>.
    /// </remarks>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task StoppedLoader_IsNotRestartedByItsIntervalTimer()
    {
        // arrange - reload every 10ms, no debounce
        var cfg = new CompositeLoaderConfig(1, 2, 5, 10, 0);
        var attempts = 0;
        var loader = Provider.CreateCompositeLoader(
            cfg,
            _ =>
            {
                Interlocked.Increment(ref attempts);
                return Task.FromResult<IBaseResult<int>>(MarketResult.Ok(1));
            }
        );

        try
        {
            // act - let the interval fire at least once, then stop
            loader.Start(true);
            await Expect.ToAsync(() => Volatile.Read(ref attempts).IsGreaterOrEqual(1));
            loader.Stop();
            var afterStop = Volatile.Read(ref attempts);

            // assert - twenty more interval ticks fit in this window; none of them may reach the fetch
            await Task.Delay(200, TestContext.Current.CancellationToken);
            Volatile
                .Read(ref attempts)
                .Is(afterStop, "a stopped loader must not be restarted by a timer that is still ticking");
        }
        finally
        {
            await loader.DisposeAsync();
        }
    }

    /// <summary>
    /// A debounced request already in flight when the loader stops does not fetch. This is the path where the
    /// state check in the callback is doing the work alone: <see cref="ICompositeLoader{T}.Stop"/> calls
    /// <c>Change</c> on the debounce timer, and that only rewrites the period — it does not cancel a firing
    /// already armed by an earlier <see cref="ICompositeLoader{T}.Request"/>. So a request followed by a stop
    /// inside the debounce window reaches the callback regardless, and the only thing between it and a fetch
    /// against a connector that believes itself stopped is that check.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task StoppedLoader_IsNotRestartedByAPendingDebounce()
    {
        // arrange - no interval, and a debounce long enough to stop inside
        var cfg = new CompositeLoaderConfig(1, 2, 5, 0, 150);
        var attempts = 0;
        var loader = Provider.CreateCompositeLoader(
            cfg,
            _ =>
            {
                Interlocked.Increment(ref attempts);
                return Task.FromResult<IBaseResult<int>>(MarketResult.Ok(1));
            }
        );

        try
        {
            // act - let the load that Start itself performs settle first, so what follows is the debounce
            // and nothing else
            loader.Start(false);
            await Expect.ToAsync(() => Volatile.Read(ref attempts).IsGreaterOrEqual(1));
            var afterStart = Volatile.Read(ref attempts);

            // arm the debounce, then stop well before it fires
            loader.Request();
            loader.Stop();

            // assert - the armed firing still arrives; it must find the loader stopped and do nothing
            await Task.Delay(400, TestContext.Current.CancellationToken);
            Volatile.Read(ref attempts).Is(afterStart, "a debounce armed before Stop must not fetch after it");
        }
        finally
        {
            await loader.DisposeAsync();
        }
    }

    /// <summary>
    /// Verifies that calling <see cref="ICompositeLoader{T}.Stop"/> after a successful load prevents a subsequent
    /// <see cref="ICompositeLoader{T}.Request"/> from reaching the fetch delegate, and that disposal reports the
    /// connecting, connected, disconnected status sequence.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task StopPreventsFurtherRequests()
    {
        var cfg = new CompositeLoaderConfig(1, 2, 5, 0, 0);
        var attempts = 0;
        var log = Get<TestLog<int>>();
        var loader = Provider.CreateCompositeLoader(
            cfg,
            _ =>
            {
                attempts++;
                return Task.FromResult<IBaseResult<int>>(MarketResult.Ok(attempts));
            }
        );
        loader.OnData += log.Add;

        loader.Start(true);
        await Expect.ToAsync(() => log.Has(1));
        attempts.Is(1);

        loader.Stop();
        var attemptsAfterStop = attempts;

        loader.Request();
        await Task.Delay(50, CancellationToken.None);
        attempts.Is(attemptsAfterStop);

        await loader.DisposeAsync();
        // three, not two: waiting for the count this assertion needs, rather than one short of it, so the
        // disconnected disposal reports has actually arrived by the time the sequence is compared
        await Expect.ToAsync(() => _statuses.Count.IsGreaterOrEqual(3));
        _statuses.ToArray().IsEqual(new[] { Connecting, Connected, Disconnected });
    }
}
