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
using Annium.Testing;
using Xunit;

namespace Annium.Finance.Providers.Core.Tests.Shared.Loaders;

/// <summary>
/// Pins that <see cref="IKeyedLoader{TKey,TContext,TData}"/> lazily creates a per-key loader on first request, and
/// threads each key's context from one successful load into the next.
/// </summary>
public class KeyedLoaderTests : TestBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="KeyedLoaderTests"/> class, registering the finance providers
    /// services and test log used to observe loaded data.
    /// </summary>
    /// <param name="outputHelper">The xUnit output helper used to capture test logs.</param>
    public KeyedLoaderTests(ITestOutputHelper outputHelper)
        : base(outputHelper)
    {
        Register(container =>
        {
            container.AddFinanceProviders();
        });
        this.RegisterTestLogs();
    }

    /// <summary>
    /// A key gets one loader however many callers ask for it at once. Creating one is not free - it binds a
    /// status reporter and starts fetching - so a second one built for the same key does not merely waste
    /// work: it is dropped from the map while still running, fetching on its own timer and holding the
    /// connector's status down, with nothing left holding a reference to stop it.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task ConcurrentRequestsForOneKey_CreateOneLoader()
    {
        // arrange - each loader counts from its own context, so a second one repeats the first event
        const int rounds = 50;
        const int callers = 16;
        var cfg = new CompositeLoaderConfig(1, 2, 5, 0, 10);
        var log = new ConcurrentQueue<(string Key, int Context, int Data)>();
        var loader = Provider.CreateKeyedLoader<string, int, int>(
            cfg,
            0,
            (_, context, _) => Task.FromResult<IBaseResult<int>>(MarketResult.Ok(context + 1)),
            (_, _, data) => data
        );

        try
        {
            loader.OnData += (key, context, data) => log.Enqueue((key, context, data));

            // act
            for (var round = 0; round < rounds; round++)
            {
                var key = $"key-{round}";
                var start = new ManualResetEventSlim();
                var callersDone = Enumerable
                    .Range(0, callers)
                    .Select(_ =>
                        Task.Run(
                            () =>
                            {
                                start.Wait(TestContext.Current.CancellationToken);
                                loader.Request(key);
                            },
                            TestContext.Current.CancellationToken
                        )
                    )
                    .ToArray();
                start.Set();
                await Task.WhenAll(callersDone);
            }

            await Task.Delay(200, TestContext.Current.CancellationToken);

            // assert - one loader per key means one event from a zero context per key
            var firsts = log.ToArray().Where(x => x.Context == 0).GroupBy(x => x.Key).ToArray();
            firsts.All(x => x.Count() == 1).IsTrue("each key must be loaded by exactly one loader");
        }
        finally
        {
            await loader.DisposeAsync();
        }
    }

    /// <summary>
    /// A new key does not drag the connector's status down while it loads. Each entry binds its own reporter,
    /// registered as connected, and starts without reporting — so a per-key refresher created at any moment
    /// stays silent. Reporting its progress instead would flash the shared monitor to connecting every time a
    /// key is first requested, and the monitor resolves connected only when every target is, so a connector
    /// would drop out of connected for reasons unrelated to its own connection.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task NewKey_DoesNotReportItsOwnProgress()
    {
        // arrange - watch the shared monitor for the whole life of the loader
        var monitor = Get<IStatusMonitor>();
        var statuses = new ConcurrentQueue<ConnectorStatus>();
        monitor.OnStatusChanged += statuses.Enqueue;

        var cfg = new CompositeLoaderConfig(1, 2, 5, 0, 10);
        var log = new ConcurrentQueue<(string Key, int Context, int Data)>();
        var loader = Provider.CreateKeyedLoader<string, int, int>(
            cfg,
            0,
            (_, context, _) => Task.FromResult<IBaseResult<int>>(MarketResult.Ok(context + 1)),
            (_, _, data) => data
        );

        try
        {
            loader.OnData += (key, context, data) => log.Enqueue((key, context, data));

            // act
            loader.Request("first");
            await Expect.ToAsync(() => log.Count.IsGreaterOrEqual(1));

            // assert - the entry registered as connected and never moved off it
            monitor.Status.Is(ConnectorStatus.Connected);
            statuses
                .Contains(ConnectorStatus.Connecting)
                .IsFalse("a per-key loader must not report its own progress to the shared monitor");
        }
        finally
        {
            await loader.DisposeAsync();
        }
    }

    /// <summary>
    /// Verifies that the first <see cref="IKeyedLoader{TKey,TContext,TData}.Request"/> for a key creates and
    /// starts its loader with the initial context, and that each subsequent successful load for that key is
    /// invoked with the context produced by the previous load.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task RequestCreatesLoaderAndUpdatesContext()
    {
        var cfg = new CompositeLoaderConfig(1, 2, 5, 0, 10);
        var log = new ConcurrentQueue<(string Key, int Context, int Data)>();
        var loader = Provider.CreateKeyedLoader<string, int, int>(
            cfg,
            0,
            (_, context, _) => Task.FromResult<IBaseResult<int>>(MarketResult.Ok(context + 1)),
            (_, _, data) => data
        );

        try
        {
            loader.OnData += (key, context, data) => log.Enqueue((key, context, data));

            // one request buys one load. How many further loads its debounce happens to coalesce into is
            // not something to assert on, so ask again for the second link rather than assuming the
            // first request produces two
            loader.Request("first");
            await Expect.ToAsync(() => log.Count.IsGreaterOrEqual(1));

            loader.Request("first");
            await Expect.ToAsync(() => log.Count.IsGreaterOrEqual(2));

            // assert the chain, not a total: the loader reloads on its own debounce, so how many events
            // have landed by the time this reads the log is a matter of timing. What must hold whatever
            // the count is - the loader starts from the initial context, and every load after that is
            // handed what the one before it produced
            var entries = log.ToArray();
            entries[0].Is(("first", 0, 1));
            for (var i = 1; i < entries.Length; i++)
            {
                entries[i].Key.Is("first");
                entries[i].Context.Is(entries[i - 1].Data, $"load {i} did not continue from load {i - 1}");
                entries[i].Data.Is(entries[i].Context + 1);
            }
        }
        finally
        {
            await loader.DisposeAsync();
        }
    }

    /// <summary>
    /// An entry reloads when it is asked to and not otherwise. Its two timing periods are adjacent arguments
    /// of the same type on the loader it builds, so handing them over the wrong way round leaves an entry that
    /// reloads on a schedule nobody configured and ignores every request - and it still produces data, from
    /// its own interval, which is what makes the swap invisible to anything that only watches for data.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task EntryWithoutAnInterval_ReloadsOnlyWhenAsked()
    {
        // arrange - the interval is zero, so every load past the entry's first must come from a request
        var cfg = new CompositeLoaderConfig(1, 2, 5, 0, 10);
        var loads = 0;
        var loader = Provider.CreateKeyedLoader<string, int, int>(
            cfg,
            0,
            (_, context, _) =>
            {
                Interlocked.Increment(ref loads);

                return Task.FromResult<IBaseResult<int>>(MarketResult.Ok(context + 1));
            },
            (_, _, data) => data
        );

        try
        {
            loader.Request("only");
            await Expect.ToAsync(() => Volatile.Read(ref loads).IsGreaterOrEqual(1));

            // act - leave it alone, asking for nothing
            await Task.Delay(150, TestContext.Current.CancellationToken);
            var settled = Volatile.Read(ref loads);
            await Task.Delay(150, TestContext.Current.CancellationToken);

            // assert
            Volatile
                .Read(ref loads)
                .Is(settled, "an entry configured without an interval reloaded without being asked");
        }
        finally
        {
            await loader.DisposeAsync();
        }
    }

    /// <summary>
    /// Disposal stops the entries the loader already built. The test beside this one covers the other half -
    /// that no new entry is built afterwards - and the two are separate mechanisms: the disposed flag refuses
    /// new entries, while draining the map is what stops the existing ones. Neither substitutes for the other,
    /// and an entry left running keeps fetching from the exchange for the life of the process.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task Disposal_StopsTheEntriesItAlreadyBuilt()
    {
        // arrange - an interval, so the entry keeps loading on its own once started and stopping it is visible
        var cfg = new CompositeLoaderConfig(1, 2, 5, 20, 0);
        var loads = 0;
        var loader = Provider.CreateKeyedLoader<string, int, int>(
            cfg,
            0,
            (_, context, _) =>
            {
                Interlocked.Increment(ref loads);

                return Task.FromResult<IBaseResult<int>>(MarketResult.Ok(context + 1));
            },
            (_, _, data) => data
        );

        loader.Request("running");
        await Expect.ToAsync(() => Volatile.Read(ref loads).IsGreaterOrEqual(3));

        // act - disposal waits for whatever load is in flight, so nothing is mid-fetch past this point
        await loader.DisposeAsync();
        var settled = Volatile.Read(ref loads);

        // assert - ten intervals' worth of silence
        await Task.Delay(200, TestContext.Current.CancellationToken);
        Volatile.Read(ref loads).Is(settled, "an entry built before disposal kept loading after it");
    }

    /// <summary>
    /// A disposed loader builds nothing. Disposal takes the entries it knows about and never runs again, so an
    /// entry created after it is unreachable and undisposable - a status reporter bound to the shared monitor
    /// for good, and a pair of timers fetching from the exchange for the life of the process. The request that
    /// does it needs no race to arrive: the connector's websocket handler calls Request from a callback, and a
    /// fill for a symbol never traded before can land at any point during teardown.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task DisposedLoader_BuildsNoFurtherEntries()
    {
        // arrange
        var cfg = new CompositeLoaderConfig(1, 2, 5, 0, 10);
        var loads = new ConcurrentQueue<string>();
        var loader = Provider.CreateKeyedLoader<string, int, int>(
            cfg,
            0,
            (key, context, _) =>
            {
                loads.Enqueue(key);

                return Task.FromResult<IBaseResult<int>>(MarketResult.Ok(context + 1));
            },
            (_, _, data) => data
        );

        loader.Request("known");
        await Expect.ToAsync(() => loads.Contains("known").IsTrue());

        // disposal waits for whatever load is in flight, so nothing can still be fetching past this point
        await loader.DisposeAsync();
        loads.Clear();

        // act - a key it already held, and one it never saw. Clearing the entries makes both of them new
        loader.Request("known");
        loader.Request("fresh");

        // assert - well past the moment a started entry would have fetched
        await Task.Delay(100, TestContext.Current.CancellationToken);
        loads.Count.Is(0, "a disposed loader must not start an entry that nothing is left to dispose");
    }

    // A commented-out StopPreventsRequestsUntilRestart used to sit here, calling Start and Stop on the
    // loader. IKeyedLoader has neither, and never did - the block would not have compiled, and it read as
    // coverage of a contract that does not exist. Stopping is not offered by design: an entry is started
    // once when its key is first requested and only ever disposed, so a keyed loader has nothing between
    // running and gone. The part of that test which is expressible - that an entry reloads only when asked -
    // is EntryWithoutAnInterval_ReloadsOnlyWhenAsked above.
}
