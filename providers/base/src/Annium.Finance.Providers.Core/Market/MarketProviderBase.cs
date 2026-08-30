using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Domain.Market;
using Annium.Finance.Providers.Abstractions.Domain.Market.Operations;
using NodaTime;

namespace Annium.Finance.Providers.Core.Market;

/// <summary>
/// Shared helpers for <see cref="Annium.Finance.Providers.Abstractions.Connectors.Market.IMarketProvider"/>
/// implementations: deriving the resource set from a list of instruments, and paging through historical candles
/// while filling any gaps a provider's response leaves in the one-minute series.
/// </summary>
public abstract class MarketProviderBase
{
    /// <summary>The duration of a single candle, in milliseconds, used to detect and fill gaps in a candle series.</summary>
    private static readonly long _minute = Duration.FromMinutes(1).TotalMilliseconds.FloorInt64();

    /// <summary>
    /// Derives the set of resources referenced by a collection of instruments (their target, quote, and
    /// currency), keeping, for each resource code, the highest-precision definition seen.
    /// </summary>
    /// <param name="instruments">The instruments to derive resources from.</param>
    /// <returns>The resources referenced by <paramref name="instruments"/>, keyed by code.</returns>
    protected Dictionary<string, ResourceModel> ResolveResources(IReadOnlyCollection<InstrumentModel> instruments)
    {
        var result = new Dictionary<string, ResourceModel>();

        foreach (var item in instruments.SelectMany(x => new[] { x.Target, x.Quote, x.Currency }))
            if (!result.TryGetValue(item.Code, out var current) || current.Precision < item.Precision)
                result[item.Code] = item;

        return result;
    }

    /// <summary>
    /// Pages through historical candles over the given time range using <paramref name="fetch"/>, yielding
    /// successive batches of consecutive one-minute candles with gaps in the provider's response filled by
    /// flat candles carried forward from the last known close. Every yielded result is a success; enumeration
    /// ends silently, without yielding a failure, as soon as a fetch fails or returns no more data, or the range
    /// has been fully covered.
    /// </summary>
    /// <param name="instrument">The instrument symbol to load candles for.</param>
    /// <param name="start">The inclusive start of the time range.</param>
    /// <param name="end">The exclusive end of the time range.</param>
    /// <param name="chunkSize">The maximum number of candles to request per fetch.</param>
    /// <param name="fetch">The provider-specific delegate that fetches a single chunk of candles starting at a given instant.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>An async sequence of candle batch results covering the requested range.</returns>
    protected async IAsyncEnumerable<MarketResult<IReadOnlyCollection<CandleModel>?>> LoadCandlesBaseAsync(
        string instrument,
        Instant start,
        Instant end,
        int chunkSize,
        Func<string, Instant, int, Task<MarketResult<List<CandleModel>?>>> fetch,
        [EnumeratorCancellation] CancellationToken ct
    )
    {
        var from = start;
        CandleModel? last = null;

        while (!ct.IsCancellationRequested)
        {
            var result = await LoadCandlesBaseAsync(instrument, from, end, chunkSize, fetch, last);

            // if failure / no data - break
            if (!result.IsSuccess || result.Data.Count == 0)
                break;

            // return result
            yield return result;

            // adjust window
            last = result.Data.Last();
            from = Instant.FromUnixTimeMilliseconds(last.Moment + _minute);

            // if window closed - break
            if (end <= from)
                break;
        }
    }

    /// <summary>
    /// Fetches a single chunk of candles starting at <paramref name="start"/> and fills any gaps left in it,
    /// including a leading gap between <paramref name="start"/> and the first fetched candle when
    /// <paramref name="last"/> is available to carry a close price forward from.
    /// </summary>
    /// <param name="instrument">The instrument symbol to load candles for.</param>
    /// <param name="start">The inclusive start of the chunk.</param>
    /// <param name="end">The exclusive end of the overall requested time range, used to cap the chunk size.</param>
    /// <param name="chunkSize">The maximum number of candles to request.</param>
    /// <param name="fetch">The provider-specific delegate that fetches a single chunk of candles.</param>
    /// <param name="last">The last candle from the previous chunk, if any, used to fill a leading gap.</param>
    /// <returns>A result containing the fetched chunk with gaps filled.</returns>
    private async Task<MarketResult<IReadOnlyCollection<CandleModel>?>> LoadCandlesBaseAsync(
        string instrument,
        Instant start,
        Instant end,
        int chunkSize,
        Func<string, Instant, int, Task<MarketResult<List<CandleModel>?>>> fetch,
        CandleModel? last
    )
    {
        var count = Math.Min((end - start).TotalMinutes, chunkSize).FloorInt32();

        // provider-specific fetch
        var result = await fetch(instrument, start, count);

        // fast return if failed
        if (!result.IsSuccess)
            return MarketResult.From(result, default(IReadOnlyCollection<CandleModel>));

        // fast return if empty
        if (result.Data.Count == 0)
            return MarketResult.Ok<IReadOnlyCollection<CandleModel>?>([]);

        var candles = result.Data;

        // fill gapes

        static CandleModel CandlePlug(long moment, decimal price) => new(moment, price, price, price, price, 0);

        var startMoment = start.ToUnixTimeMilliseconds();
        if (last is not null && candles[0].Moment != startMoment)
            candles.Insert(0, CandlePlug(startMoment, last.Close));

        for (var i = 1; i < candles.Count; i++)
        {
            var prev = candles[i - 1];
            var curr = candles[i];

            if (curr.Moment - prev.Moment > _minute)
                candles.Insert(i, CandlePlug(prev.Moment + _minute, prev.Close));
        }

        // return processed candles
        return MarketResult.Ok<IReadOnlyCollection<CandleModel>?>(candles);
    }
}
