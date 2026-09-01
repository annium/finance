using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Domain.Market;
using Annium.Finance.Providers.Abstractions.Domain.Market.Operations;
using Annium.Finance.Providers.Core.Market;
using Annium.Testing;
using NodaTime;
using Xunit;

namespace Annium.Finance.Providers.Core.Tests.Market;

/// <summary>
/// Pins how <see cref="MarketProviderBase"/> pages through a historical candle range: what it does with a
/// fetch that fails partway, and how it fills gaps a provider leaves in the one-minute series.
/// </summary>
public class MarketProviderBaseTests
{
    /// <summary>The moment the ranges in these tests start at.</summary>
    private static readonly Instant _start = Instant.FromUnixTimeMilliseconds(1_700_000_000_000);

    /// <summary>
    /// A fetch that fails partway through the range is handed to the caller before the enumeration ends.
    /// Ending silently made a range truncated by, say, a rate limit indistinguishable from one that was fully
    /// covered - the series simply stopped early and looked finished.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task FailedFetch_IsYieldedBeforeTheRangeEnds()
    {
        // arrange - the first chunk arrives, the second fails
        var provider = new TestMarketProvider();
        var call = 0;

        Task<MarketResult<List<CandleModel>?>> Fetch(string symbol, Instant from, int count)
        {
            call++;

            return Task.FromResult(
                call == 1
                    ? MarketResult.Ok<List<CandleModel>?>(Candles(from, 5))
                    : MarketResult.New<List<CandleModel>?>(MarketOperationStatus.NetworkError, null, "boom")
            );
        }

        // act
        var batches = await provider
            .LoadAsync(_start, _start + Duration.FromMinutes(60), Fetch, TestContext.Current.CancellationToken)
            .ToListAsync(TestContext.Current.CancellationToken);

        // assert
        batches.Count.Is(2);
        batches[0].Status.Is(MarketOperationStatus.Ok);
        batches[1]
            .Status.Is(
                MarketOperationStatus.NetworkError,
                "a range cut short by a failed fetch must say so, not just stop"
            );
    }

    /// <summary>
    /// A range the provider covers to its end finishes without a failure batch, so a complete history is not
    /// mistaken for a truncated one.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task CoveredRange_EndsWithoutAFailure()
    {
        // arrange - one chunk covers the whole range
        var provider = new TestMarketProvider();
        var call = 0;

        Task<MarketResult<List<CandleModel>?>> Fetch(string symbol, Instant from, int count)
        {
            call++;

            return Task.FromResult(MarketResult.Ok<List<CandleModel>?>(Candles(from, count)));
        }

        // act
        var batches = await provider
            .LoadAsync(_start, _start + Duration.FromMinutes(5), Fetch, TestContext.Current.CancellationToken)
            .ToListAsync(TestContext.Current.CancellationToken);

        // assert
        batches.Count.Is(1);
        batches[0].Status.Is(MarketOperationStatus.Ok);
        batches[0].Data.NotNull().Count.Is(5);

        // and the provider is asked exactly once. Counting batches is not enough to see this: a chunk
        // landing exactly on the boundary that failed to end the paging would go round once more, come
        // back empty, and stop there - same batch count, one wasted request against the exchange's limit
        call.Is(1, "a range already covered must not be fetched again");
    }

    /// <summary>
    /// A provider that runs out of data before the range is covered ends the enumeration rather than asking
    /// again forever. An empty answer is how a provider says it has nothing further, and it is not a failure —
    /// so the range simply ends, with no failure batch to report.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task EmptyFetchMidRange_EndsTheEnumeration()
    {
        // arrange - five candles, then nothing, for a range wanting sixty
        var provider = new TestMarketProvider();
        var call = 0;

        Task<MarketResult<List<CandleModel>?>> Fetch(string symbol, Instant from, int count)
        {
            call++;

            return Task.FromResult(MarketResult.Ok<List<CandleModel>?>(call == 1 ? Candles(from, 5) : []));
        }

        // act
        var batches = await provider
            .LoadAsync(_start, _start + Duration.FromMinutes(60), Fetch, TestContext.Current.CancellationToken)
            .ToListAsync(TestContext.Current.CancellationToken);

        // assert
        batches.Count.Is(1, "the empty answer ends the range instead of being yielded or retried");
        batches[0].Status.Is(MarketOperationStatus.Ok);
        call.Is(2, "the provider is asked once more, and its empty answer stops the paging");
    }

    /// <summary>
    /// A minute the provider skipped is filled with a flat candle carried forward from the previous close, so
    /// the series a caller receives is contiguous.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task GapsInAChunk_AreFilledFromTheLastClose()
    {
        // arrange - minutes 0 and 3 only; 1 and 2 are missing
        var provider = new TestMarketProvider();
        var minute = Duration.FromMinutes(1);

        Task<MarketResult<List<CandleModel>?>> Fetch(string symbol, Instant from, int count) =>
            Task.FromResult(MarketResult.Ok<List<CandleModel>?>([Candle(from, 10m), Candle(from + minute * 3, 20m)]));

        // act
        var batches = await provider
            .LoadAsync(_start, _start + Duration.FromMinutes(4), Fetch, TestContext.Current.CancellationToken)
            .ToListAsync(TestContext.Current.CancellationToken);

        // assert
        var candles = batches[0].Data.NotNull().ToArray();
        candles.Length.Is(4);
        candles
            .Select(x => x.Moment)
            .ToArray()
            .IsEqual(Enumerable.Range(0, 4).Select(i => (_start + minute * i).ToUnixTimeMilliseconds()).ToArray());
        candles[1].Close.Is(10m, "a filled minute carries the last known close forward");
        candles[2].Close.Is(10m);
        candles[3].Close.Is(20m);
    }

    /// <summary>
    /// A gap between one chunk and the next is filled too, carrying the previous chunk's close forward. This
    /// is a separate branch from the within-chunk fill, and the one nothing reached: it needs a second fetch
    /// whose first candle starts later than the minute the paging asked for, which only happens once a chunk
    /// has already been paged. Left unfilled, every chunk boundary the provider skipped would leave a hole no
    /// consumer of a one-minute series expects.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task GapBetweenChunks_IsFilledFromThePreviousChunksClose()
    {
        // arrange - minutes 0 and 1 at 10, then a chunk that resumes at minute 4 at 20, skipping 2 and 3
        var provider = new TestMarketProvider();
        var minute = Duration.FromMinutes(1);
        var call = 0;

        Task<MarketResult<List<CandleModel>?>> Fetch(string symbol, Instant from, int count)
        {
            call++;

            return Task.FromResult(
                MarketResult.Ok<List<CandleModel>?>(
                    call switch
                    {
                        1 => [Candle(_start, 10m), Candle(_start + minute, 10m)],
                        2 => [Candle(_start + minute * 4, 20m), Candle(_start + minute * 5, 20m)],
                        _ => [],
                    }
                )
            );
        }

        // act
        var batches = await provider
            .LoadAsync(_start, _start + Duration.FromMinutes(10), Fetch, TestContext.Current.CancellationToken)
            .ToListAsync(TestContext.Current.CancellationToken);

        // assert - the second batch opens at the minute the paging asked for, not at the one the provider
        // answered with, and the minutes between are carried forward from the first batch's close
        var candles = batches[1].Data.NotNull().ToArray();
        candles.Length.Is(4, "the gap across the chunk boundary was not filled");
        candles
            .Select(x => x.Moment)
            .ToArray()
            .IsEqual(Enumerable.Range(2, 4).Select(i => (_start + minute * i).ToUnixTimeMilliseconds()).ToArray());
        candles[0].Close.Is(10m, "the plug carries the previous chunk's close, not the next one's open");
        candles[1].Close.Is(10m);
        candles[2].Close.Is(20m);
    }

    /// <summary>
    /// A provider that answers with the same page forever is stopped and reported, rather than paged until
    /// something else gives out. Nothing else in the loop closes it: the answer is neither empty nor a
    /// failure, so both of the other exits stay shut and the window never moves.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ProviderThatNeverAdvances_IsStoppedAndReported()
    {
        // arrange - always the same candle, whatever was asked for
        var provider = new TestMarketProvider();
        var call = 0;

        Task<MarketResult<List<CandleModel>?>> Fetch(string symbol, Instant from, int count)
        {
            call++;

            return Task.FromResult(MarketResult.Ok<List<CandleModel>?>([Candle(_start, 10m)]));
        }

        // act
        var batches = await provider
            .LoadAsync(_start, _start + Duration.FromMinutes(60), Fetch, TestContext.Current.CancellationToken)
            .ToListAsync(TestContext.Current.CancellationToken);

        // assert - it stops, and the last thing the caller gets says why. The count is three rather than
        // two because the gap fill plugs the minute the second request asked for and did not receive,
        // which advances the window once before it stalls for good
        batches[^1]
            .Status.Is(
                MarketOperationStatus.UnknownError,
                "a range that stopped advancing must say so rather than be paged forever"
            );
        batches.Take(batches.Count - 1).All(x => x.Status == MarketOperationStatus.Ok).IsTrue();
        (call < 5).IsTrue($"the loop asked {call} times before stopping");
    }

    /// <summary>
    /// Deriving the resource set from a list of instruments keeps, for each asset code, the definition
    /// carrying the most decimal digits. The same asset appears on many instruments and providers do not
    /// always report it at the same precision; keeping the coarser one would round every amount in that
    /// asset short of what the exchange actually quotes.
    /// </summary>
    [Fact]
    public void ResolveResources_KeepsTheHighestPrecisionPerCode()
    {
        // arrange - BTC seen at 2 decimals on one instrument and 8 on another
        var provider = new TestMarketProvider();
        var instruments = new[]
        {
            Instrument("BTCUSDT", new ResourceModel("BTC", 2), new ResourceModel("USDT", 2)),
            Instrument("BTCEUR", new ResourceModel("BTC", 8), new ResourceModel("EUR", 4)),
        };

        // act
        var resources = provider.Resolve(instruments);

        // assert
        resources.Count.Is(3);
        resources["BTC"].Precision.Is((byte)8, "the finer definition of an asset is the one to keep");
        resources["USDT"].Precision.Is((byte)2);
        resources["EUR"].Precision.Is((byte)4);
    }

    /// <summary>
    /// Builds an instrument carrying the given resources, with limits these tests do not depend on.
    /// </summary>
    /// <param name="symbol">The instrument's symbol.</param>
    /// <param name="target">The base resource.</param>
    /// <param name="quote">The quote resource, also used as the settlement currency.</param>
    /// <returns>The instrument.</returns>
    private static InstrumentModel Instrument(string symbol, ResourceModel target, ResourceModel quote) =>
        new(symbol, target, quote, quote, 1m, 100m, 1m, 1m, 100m, 1m, 1m, decimal.MaxValue, int.MaxValue);

    /// <summary>
    /// Builds a run of consecutive one-minute candles starting at the given moment.
    /// </summary>
    /// <param name="from">The moment the first candle covers.</param>
    /// <param name="count">The number of candles to build.</param>
    /// <returns>The candles, in chronological order.</returns>
    private static List<CandleModel> Candles(Instant from, int count) =>
        Enumerable.Range(0, count).Select(i => Candle(from + Duration.FromMinutes(i), 10m)).ToList();

    /// <summary>
    /// Builds a flat one-minute candle at the given moment.
    /// </summary>
    /// <param name="moment">The moment the candle covers.</param>
    /// <param name="price">The price every one of the candle's OHLC values takes.</param>
    /// <returns>The candle.</returns>
    private static CandleModel Candle(Instant moment, decimal price) =>
        new(moment.ToUnixTimeMilliseconds(), price, price, price, price, 1m);

    /// <summary>
    /// Exposes <see cref="MarketProviderBase.LoadCandlesBaseAsync"/>, which is protected, to these tests.
    /// </summary>
    private sealed class TestMarketProvider : MarketProviderBase
    {
        /// <summary>
        /// Pages through the given range with the given fetch.
        /// </summary>
        /// <param name="start">The inclusive start of the range.</param>
        /// <param name="end">The exclusive end of the range.</param>
        /// <param name="fetch">The fetch answering each chunk.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>The batches the base class yields for the range.</returns>
        public IAsyncEnumerable<MarketResult<IReadOnlyCollection<CandleModel>?>> LoadAsync(
            Instant start,
            Instant end,
            Func<string, Instant, int, Task<MarketResult<List<CandleModel>?>>> fetch,
            CancellationToken ct
        ) => LoadCandlesBaseAsync("XY", start, end, 1000, fetch, ct);

        /// <summary>
        /// Exposes <see cref="MarketProviderBase.ResolveResources"/>, which is protected, to these tests.
        /// </summary>
        /// <param name="instruments">The instruments to derive resources from.</param>
        /// <returns>The resources, keyed by asset code.</returns>
        public Dictionary<string, ResourceModel> Resolve(IReadOnlyCollection<InstrumentModel> instruments) =>
            ResolveResources(instruments);
    }
}
