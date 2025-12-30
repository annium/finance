using System.Collections.Concurrent;
using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Domain.Market.Operations;
using Annium.Finance.Providers.Abstractions.Domain.Shared.Operations;
using Annium.Finance.Providers.Core.Shared;
using Annium.Finance.Providers.Core.Shared.Loaders;
using Annium.Testing;
using Xunit;

namespace Annium.Finance.Providers.Core.Tests.Shared.Loaders;

public class KeyedLoaderTests : TestBase
{
    public KeyedLoaderTests(ITestOutputHelper outputHelper)
        : base(outputHelper)
    {
        Register(container =>
        {
            container.AddFinanceProviders();
        });
        this.RegisterTestLogs();
    }

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
