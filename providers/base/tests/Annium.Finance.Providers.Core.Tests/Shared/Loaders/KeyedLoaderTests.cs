using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Domain.Market.Operations;
using Annium.Finance.Providers.Abstractions.Domain.Shared.Operations;
using Annium.Finance.Providers.Core.Shared;
using Annium.Finance.Providers.Core.Shared.Loaders;
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

            loader.Request("first");
            await Expect.ToAsync(() => log.Count.IsGreaterOrEqual(2));

            loader.Request("first");
            await Expect.ToAsync(() => log.Count.IsGreaterOrEqual(3));

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

    // [Fact]
    // public async Task StopPreventsRequestsUntilRestart()
    // {
    //     var cfg = new CompositeLoaderConfig(1, 2, 5, 0, 5);
    //     var attempts = 0;
    //     var loader = Provider.CreateKeyedLoader<string, int, int>(
    //         cfg,
    //         0,
    //         (_, context, _) =>
    //         {
    //             Interlocked.Increment(ref attempts);
    //             return Task.FromResult<IBaseResult<int>>(MarketResult.Ok(context + 1));
    //         },
    //         (_, _, data) => data
    //     );
    //
    //     try
    //     {
    //         loader.Request("key");
    //         await Expect.ToAsync(() => attempts.Is(1));
    //
    //         loader.Start(true);
    //         loader.Stop();
    //
    //         var attemptsAfterStop = attempts;
    //         loader.Request("key");
    //         await Task.Delay(30, CancellationToken.None);
    //         attempts.Is(attemptsAfterStop);
    //
    //         loader.Start(true);
    //         loader.Request("key");
    //         await Expect.ToAsync(() => attempts.IsGreater(attemptsAfterStop));
    //     }
    //     finally
    //     {
    //         await loader.DisposeAsync();
    //     }
    // }
}
