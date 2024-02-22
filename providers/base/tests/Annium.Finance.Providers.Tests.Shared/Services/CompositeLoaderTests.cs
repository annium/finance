using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Annium.Core.DependencyInjection;
using Annium.Finance.Providers.Abstractions.Connectors.Connectors;
using Annium.Finance.Providers.Abstractions.Domain.Operations;
using Annium.Finance.Providers.Shared.Connectors;
using Annium.Finance.Providers.Shared.Loaders;
using Annium.Linq;
using Annium.Logging;
using Annium.Testing;
using Xunit;
using Xunit.Abstractions;
using static Annium.Finance.Providers.Abstractions.Connectors.Connectors.ConnectorStatus;

namespace Annium.Finance.Providers.Tests.Shared.Services;

public class CompositeLoaderTests : TestBase
{
    private readonly ConcurrentQueue<ConnectorStatus> _statuses = new();

    public CompositeLoaderTests(ITestOutputHelper outputHelper)
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
        using var loader = Get<ILoaderFactory>().CreateCompositeLoader<int>(cfg, async _ => await Load());
        loader.OnData += log.Add;

        loader.Start(true);
        for (var i = 0; i < 10; i++)
            loader.Request();

        await Expect.To(() => log.Has(1));
        log.At(0).Is(3);
        this.Trace<string>("statuses: {statuses}", _statuses.Select(x => x.ToString()).Join(", "));
        _statuses.Has(2);
        _statuses.At(0).Is(Connecting);
        _statuses.At(1).Is(Connected);
    }
}
