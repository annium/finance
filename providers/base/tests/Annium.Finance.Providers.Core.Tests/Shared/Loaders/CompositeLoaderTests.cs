using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Connectors.Shared;
using Annium.Finance.Providers.Abstractions.Domain.Market.Operations;
using Annium.Finance.Providers.Core.Shared;
using Annium.Finance.Providers.Core.Shared.Loaders;
using Annium.Finance.Providers.Core.Shared.Status;
using Annium.Linq;
using Annium.Logging;
using Annium.Testing;
using Xunit;
using static Annium.Finance.Providers.Abstractions.Connectors.Shared.ConnectorStatus;

namespace Annium.Finance.Providers.Core.Tests.Shared.Loaders;

public class CompositeLoaderTests : TestBase
{
    private readonly ConcurrentQueue<ConnectorStatus> _statuses = new();

    public CompositeLoaderTests(ITestOutputHelper outputHelper)
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

        var statuses = Array.Empty<ConnectorStatus>();
        await Expect.ToAsync(() =>
        {
            statuses = _statuses.ToArray();
            log.Has(1);
        });
        log.At(0).Is(3);
        this.Trace<string>("statuses: {statuses}", statuses.Select(x => x.ToString()).Join(", "));
        statuses.ToArray().IsEqual(new[] { Connecting, Connected });
    }
}
