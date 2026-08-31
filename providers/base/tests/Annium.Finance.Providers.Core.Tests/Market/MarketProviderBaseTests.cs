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

        Task<MarketResult<List<CandleModel>?>> Fetch(string symbol, Instant from, int count) =>
            Task.FromResult(MarketResult.Ok<List<CandleModel>?>(Candles(from, count)));

        // act
        var batches = await provider
            .LoadAsync(_start, _start + Duration.FromMinutes(5), Fetch, TestContext.Current.CancellationToken)
            .ToListAsync(TestContext.Current.CancellationToken);

        // assert - one batch and no more: a chunk landing exactly on the range boundary ends the paging
        // rather than going round again for a window that is already closed
        batches.Count.Is(1, "a covered range must not be fetched past its end");
        batches[0].Status.Is(MarketOperationStatus.Ok);
        batches[0].Data.NotNull().Count.Is(5);
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
    }
}
