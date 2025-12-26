using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Connectors.Market;
using Annium.Finance.Providers.Abstractions.Domain.Market;
using Annium.Finance.Providers.Abstractions.Domain.Market.Operations;
using Annium.Finance.Providers.Abstractions.Domain.Shared;
using Annium.Finance.Providers.Core.Market;
using Annium.Finance.Providers.Core.Shared.Status;
using Annium.Finance.Providers.Tests.Lib;
using Annium.Logging;
using Annium.Testing;
using Annium.Threading.Tasks;
using NodaTime;
using Xunit;

namespace Annium.Finance.Providers.Core.Tests.Market;

public class MarketConnectorBaseTests : ProvidersTestBase
{
    public MarketConnectorBaseTests(ITestOutputHelper outputHelper)
        : base(outputHelper) { }

    [Fact]
    public async Task Sync_Works()
    {
        // arrange
        this.Trace("prepare settings");
        var settings = new MarketSettings { Provider = "fake", Environment = ProviderEnvironment.Test };

        this.Trace("prepare data");
        const int dataSize = 1000;
        var resources = new[]
        {
            new ResourceModel("BTC", 8),
            new ResourceModel("USDT", 8),
            new ResourceModel("BNB", 8),
        };
        var instruments = new[]
        {
            new InstrumentModel(
                "BTCUSDT",
                resources[0],
                resources[1],
                resources[1],
                0.001m,
                100m,
                0.001m,
                1m,
                100000m,
                0.01m,
                10m,
                1000000m,
                100
            ),
        };

        var tickers = Enumerable
            .Range(0, dataSize)
            .Select(i => new InstrumentTicker(instruments[0].Symbol, i, i))
            .ToArray();

        this.Trace("create connector");
        await using var market = CreateConnector(settings);

        this.Trace("setup sync handler");
        market.OnSync += async (s, res, ins) =>
        {
            s.Is(settings);
            res.SequenceEqual(resources).IsTrue();
            ins.SequenceEqual(instruments).IsTrue();

            this.Trace("sync:start");
            await Task.Delay(200);
            this.Trace("sync:done");
        };

        this.Trace("subscribe to tickers");
        var tickerLog = new TestLog<int>();
        market.Tickers.Subscribe(t => tickerLog.Add((int)t.BidPrice));

        this.Trace("emit tickers");
        Emit(tickers, market.Ticker);

        this.Trace("trigger sync");
        market.Sync(resources, instruments);

        // assert
        this.Trace("await for all events");
        await Wait.UntilAsync(() => tickerLog.Count == dataSize);

        this.Trace("verify tickers log");
        VerifyLog("tickers", tickerLog);
        market.Resources.SequenceEqual(resources).IsTrue();
        market.Instruments.SequenceEqual(instruments).IsTrue();
    }

    private FakeMarketConnector CreateConnector(MarketSettings settings)
    {
        var provider = new FakeMarketProvider();
        var reporter = Get<IStatusReporter>();
        var monitor = Get<IStatusMonitor>();

        return new FakeMarketConnector(settings, provider, reporter, monitor, Logger);
    }

    private void Emit(IReadOnlyList<InstrumentTicker> data, Action<InstrumentTicker> emit)
    {
        Task.Run(
                async () =>
                {
                    await Task.Delay(10);
                    foreach (var x in data)
                    {
                        await Task.Delay(1);
                        emit(x);
                    }
                },
                TestContext.Current.CancellationToken
            )
            .GetAwaiter();
    }

    private void VerifyLog(string type, TestLog<int> log)
    {
        this.Trace<string>("verify {type} log", type);
        var entries = log.ToArray();
        try
        {
            for (var i = 1; i < entries.Length - 1; i++)
                entries[i].Is(entries[i - 1] + 1);
        }
        catch
        {
            this.Error<string>("{type} log is not as expected:", type);
            for (var i = 0; i < entries.Length - 1; i++)
                this.Trace("{entry}", entries[i]);
            throw;
        }
    }

    private class FakeMarketConnector : MarketConnectorBase
    {
        public FakeMarketConnector(
            MarketSettings settings,
            IMarketProvider provider,
            IStatusReporter reporter,
            IStatusMonitor monitor,
            ILogger logger
        )
            : base(settings, provider, reporter, monitor, Annium.Disposable.AsyncBox(logger), logger) { }

        public void Sync(IReadOnlyCollection<ResourceModel> resources, IReadOnlyCollection<InstrumentModel> instruments)
        {
            ScheduleSync(resources, instruments);
        }

        public void Ticker(InstrumentTicker ticker)
        {
            Write(ticker);
        }
    }

    private class FakeMarketProvider : IMarketProvider
    {
        public Task<MarketResult<MarketContext?>> LoadContextAsync()
        {
            throw new NotImplementedException();
        }

        public IAsyncEnumerable<MarketResult<IReadOnlyCollection<CandleModel>?>> LoadCandlesAsync(
            string instrument,
            Instant start,
            Instant end,
            CancellationToken ct
        )
        {
            throw new NotImplementedException();
        }
    }
}
