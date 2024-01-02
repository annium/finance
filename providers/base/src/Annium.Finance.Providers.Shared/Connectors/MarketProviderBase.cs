using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Domain.Models;
using Annium.Finance.Providers.Abstractions.Domain.Operations;
using NodaTime;

namespace Annium.Finance.Providers.Shared.Connectors;

public abstract class MarketProviderBase
{
    private static readonly long Minute = Duration.FromMinutes(1).TotalMilliseconds.FloorInt64();

    protected IReadOnlyCollection<ResourceModel> ResolveResources(IReadOnlyCollection<InstrumentModel> instruments)
    {
        var result = new Dictionary<string, ResourceModel>();

        foreach (var item in instruments.SelectMany(x => new[] { x.Target, x.Quote, x.Currency }))
            if (!result.TryGetValue(item.Code, out var current) || current.Precision < item.Precision)
                result[item.Code] = item;

        return result.Values;
    }

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
            if (result.IsFailure || result.Data.Count == 0)
                break;

            // return result
            yield return result;

            // adjust window
            last = result.Data.Last();
            from = Instant.FromUnixTimeMilliseconds(last.Moment + Minute);

            // if window closed - break
            if (end <= from)
                break;
        }
    }

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
        if (result.IsFailure)
            return MarketResult.From(result, default(IReadOnlyCollection<CandleModel>));

        // fast return if empty
        if (result.Data.Count == 0)
            return MarketResult.Ok<IReadOnlyCollection<CandleModel>?>(Array.Empty<CandleModel>());

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

            if (curr.Moment - prev.Moment > Minute)
                candles.Insert(i, CandlePlug(prev.Moment + Minute, prev.Close));
        }

        // return processed candles
        return MarketResult.Ok<IReadOnlyCollection<CandleModel>?>(candles);
    }
}
