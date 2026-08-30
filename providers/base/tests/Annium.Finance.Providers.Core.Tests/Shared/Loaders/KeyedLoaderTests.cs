using System.Collections.Concurrent;
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
            await Expect.ToAsync(() => log.Has(2));
            var first = log.ToArray()[0];
            first.Is(("first", 0, 1));

            loader.Request("first");
            await Expect.ToAsync(() => log.Has(3));
            var entries = log.ToArray();
            entries.Length.Is(3);
            var last = entries[2];
            last.Is(("first", 2, 3));
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
