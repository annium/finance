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
        await Expect.ToAsync(() => _statuses.Count.IsGreaterOrEqual(2));
        _statuses.ToArray().IsEqual(new[] { Connecting, Connected, Disconnected });
    }
}
