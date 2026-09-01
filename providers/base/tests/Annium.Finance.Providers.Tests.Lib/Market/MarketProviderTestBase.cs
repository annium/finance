using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Connectors.Market;
using Annium.Finance.Providers.Abstractions.Domain.Market;
using Annium.Finance.Providers.Abstractions.Domain.Market.Operations;
using Annium.Finance.Providers.Abstractions.Domain.Shared;
using Annium.Logging;
using Annium.NodaTime.Extensions;
using Annium.Testing;
using NodaTime;
using Xunit;

namespace Annium.Finance.Providers.Tests.Lib.Market;

/// <summary>
/// Base for tests that resolve a provider's market data provider (request/response, not streaming) and
/// check that it can load context and two days of one-minute candles for a fixed symbol. Read-only.
/// </summary>
public abstract class MarketProviderTestBase : ProvidersTestBase
{
    /// <summary>The symbol the derived test loads candles for.</summary>
    private readonly string _symbol;

    /// <summary>
    /// Initializes a new instance of the <see cref="MarketProviderTestBase"/> class.
    /// </summary>
    /// <param name="symbol">The symbol to load candles for.</param>
    /// <param name="outputHelper">The xUnit output helper to route trace logging to.</param>
    protected MarketProviderTestBase(string symbol, ITestOutputHelper outputHelper)
        : base(outputHelper)
    {
        _symbol = symbol;
    }

    /// <summary>
    /// Resolves the market provider for the given provider/environment, loads its context and asserts it is
    /// populated, then loads the last two days of one-minute candles for the configured symbol and asserts
    /// the expected count and that the first/last candles carry real OHLC data.
    /// </summary>
    /// <param name="providerKey">The provider and environment to resolve the market provider for.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected async Task MarketProviderBaseAsync(ProviderKey providerKey)
    {
        this.Trace("start");

        // arrange
        var keys = Get<IEnumerable<ProviderKey>>().ToArray();
        keys.Contains(providerKey).IsTrue();

        // act - resolve market provider
        this.Trace("resolve market provider");
        var providerFactory = GetKeyed<IMarketProviderFactory>(providerKey.Provider);
        var settings = new MarketSettings { Provider = providerKey.Provider };
        var provider = providerFactory.Create(settings);

        // act - load context
        var context = await provider.LoadContextAsync();

        // assert - context
        context.Status.Is(MarketOperationStatus.Ok);
        var data = context.Data;
        data.IsNotDefault();
        data.Resources.Count.IsGreater(0);
        data.Instruments.Count.IsGreater(0);

        // act - load candles
        var end = SystemClock.Instance.GetCurrentInstant().FloorToMinute();
        var start = end - Duration.FromDays(2);
        var candles = new List<CandleModel>();
        this.Trace("load candles in for {symbol} ({key}) in {start} - {end}", _symbol, providerKey, start, end);
        await foreach (var chunkResult in provider.LoadCandlesAsync(_symbol, start, end, CancellationToken.None))
        {
            chunkResult.IsSuccess.IsTrue();
            candles.AddRange(chunkResult.Data.NotNull());
        }

        // assert - candles
        this.Trace("verify candles");
        candles.Count.Is(2880);
        var firstCandle = candles[0];
        firstCandle.Moment.Is(start.ToUnixTimeMilliseconds());
        firstCandle.Open.IsNotDefault();
        firstCandle.High.IsNotDefault();
        firstCandle.Low.IsNotDefault();
        firstCandle.Close.IsNotDefault();
        // the last one, not candles[0] again: reading the first twice left the whole tail of the series
        // - every candle the paging loop fetched after the opening chunk - asserted by nothing at all
        var lastCandle = candles[^1];
        lastCandle.Moment.Is((end - Duration.FromMinutes(1)).ToUnixTimeMilliseconds());
        lastCandle.Open.IsNotDefault();
        lastCandle.High.IsNotDefault();
        lastCandle.Low.IsNotDefault();
        lastCandle.Close.IsNotDefault();

        this.Trace("done");
    }
}
