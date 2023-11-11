using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Annium.Core.DependencyInjection;
using Annium.Data.Operations;
using Annium.Finance.Providers.Abstractions.Connectors.Connectors;
using Annium.Finance.Providers.Shared.Connectors;
using Annium.Finance.Providers.Shared.Services;
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
            container.AddProvidersSingleton();
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
        async Task<IResult<int>> Load(CancellationToken ct)
        {
            attempt++;

            await Task.Delay(5, CancellationToken.None);

            return attempt < 10 ? Result.New(0).Error($"No data at {attempt}") : Result.New(attempt++);
        }
        using var loader = new SnapshotLoader<int>(cfg, Load, Get<IStatusReporter>(), Logger);
        loader.OnFetched += log.Add;

        loader.Start();

        await Expect.To(() => log.Has(1));
        log.At(0).Is(10);
        _statuses.IsEqual(new[] { Connecting, Connected });
    }
}
