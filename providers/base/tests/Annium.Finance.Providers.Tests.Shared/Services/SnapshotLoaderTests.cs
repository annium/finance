using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Annium.Core.DependencyInjection;
using Annium.Finance.Providers.Abstractions.Connectors.Connectors;
using Annium.Finance.Providers.Abstractions.Domain.Operations;
using Annium.Finance.Providers.Shared.Connectors;
using Annium.Finance.Providers.Shared.Loaders;
using Annium.Testing;
using Xunit;
using Xunit.Abstractions;
using static Annium.Finance.Providers.Abstractions.Connectors.Connectors.ConnectorStatus;

namespace Annium.Finance.Providers.Tests.Shared.Services;

public class SnapshotLoaderTests : TestBase
{
    private readonly ConcurrentQueue<ConnectorStatus> _statuses = new();

    public SnapshotLoaderTests(ITestOutputHelper outputHelper)
        : base(outputHelper)
    {
        Register(container =>
        {
            container.AddProviders();
        });
        RegisterTestLogs();

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
        using var loader = Get<ILoaderFactory>().CreateSnapshotLoader<int>(cfg, async _ => await Load());
        loader.OnData += log.Add;

        loader.Start(true);

        await Expect.To(() => log.Has(1));
        log.At(0).Is(10);
        _statuses.IsEqual(new[] { Connecting, Connected });
    }
}
