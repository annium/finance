using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Domain.Dto;
using Annium.Finance.Providers.Abstractions.Domain.Operations;
using Annium.NodaTime.Extensions;
using NodaTime;

namespace Annium.Finance.Providers.Shared.Connectors;

public abstract class MarketProviderBase
{
    private static readonly Duration Minute = Duration.FromMinutes(1);

    protected IReadOnlyCollection<ResourceDto> ResolveResources(IReadOnlyCollection<InstrumentDto> instruments)
    {
        var result = new Dictionary<string, ResourceDto>();

        foreach (var item in instruments.SelectMany(x => new[] { x.Target, x.Quote, x.Currency }))
            if (!result.TryGetValue(item.Code, out var current) || current.Precision < item.Precision)
                result[item.Code] = item;

        return result.Values;
    }

    protected async IAsyncEnumerable<MarketResult<IReadOnlyCollection<CandleDto>>> LoadCandlesBaseAsync(
        string instrument,
        Instant start,
        Instant end,
        int chunkSize,
        Func<string, Instant, int, Task<MarketResult<List<CandleDto>>>> fetch,
        [EnumeratorCancellation] CancellationToken ct
    )
    {
        var from = start;
        CandleDto? last = null;

        while (!ct.IsCancellationRequested)
        {
            var result = await LoadCandlesBaseAsync(instrument, from, end, chunkSize, fetch, last);

            // return result
            yield return result;

            // if failure / no data - break
            if (result.IsFailure || result.Data.Count == 0)
                break;

            // adjust window
            last = result.Data.Last();
            from = last.Moment.Plus(Duration.FromMinutes(1)).FloorToMinute();

            // if window closed - break
            if (end <= from)
                break;
        }
    }

    private async Task<MarketResult<IReadOnlyCollection<CandleDto>>> LoadCandlesBaseAsync(
        string instrument,
        Instant start,
        Instant end,
        int chunkSize,
        Func<string, Instant, int, Task<MarketResult<List<CandleDto>>>> fetch,
        CandleDto? last
    )
    {
        var count = Math.Min((end - start).TotalMinutes, chunkSize).FloorInt32();

        // provider-specific fetch
        var result = await fetch(instrument, start, count);

        // fast return if failed or empty
        if (result.IsFailure || result.Data.Count == 0)
            return MarketResult.New<IReadOnlyCollection<CandleDto>>(
                result.Status,
                Array.Empty<CandleDto>(),
                result.Message
            );

        var candles = result.Data;

        // fill gapes

        static CandleDto CandlePlug(Instant moment, decimal price) => new(moment, price, price, price, price, 0);

        if (last is not null && candles[0].Moment != start)
            candles.Insert(0, CandlePlug(start, last.Close));

        for (var i = 1; i < candles.Count; i++)
        {
            var prev = candles[i - 1];
            var curr = candles[i];

            if (curr.Moment - prev.Moment > Minute)
                candles.Insert(i, CandlePlug(prev.Moment + Minute, prev.Close));
        }

        // return processed candles
        return MarketResult.Ok<IReadOnlyCollection<CandleDto>>(candles);
    }
}
