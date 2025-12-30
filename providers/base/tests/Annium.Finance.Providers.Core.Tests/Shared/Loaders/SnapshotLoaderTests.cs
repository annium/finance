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

public class SnapshotLoaderTests : TestBase
{
    private readonly ConcurrentQueue<ConnectorStatus> _statuses = new();

    public SnapshotLoaderTests(ITestOutputHelper outputHelper)
        : base(outputHelper)
    {
        Register(container =>
        {
            container.AddFinanceProviders();
        });
        this.RegisterTestLogs();

        var monitor = Get<IStatusMonitor>();
        monitor.OnStatusChanged += _statuses.Enqueue;
    }

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
