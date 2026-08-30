using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Connectors.Market;
using Annium.Finance.Providers.Abstractions.Domain.Market;
using Annium.Finance.Providers.Abstractions.Domain.Market.Operations;
using Annium.Finance.Providers.Core.Market;
using Annium.Finance.Providers.Core.Shared.RateLimits;
using Annium.Finance.Providers.Crypto.Binance.Base.Shared.HttpExtensions;
using Annium.Finance.Providers.Crypto.Binance.Spot.Internal.Market.Contracts.Domain;
using Annium.Finance.Providers.Crypto.Binance.Spot.Internal.Market.HttpExtensions;
using Annium.Logging;
using Annium.Net.Http;
using NodaTime;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Internal.Market;

/// <summary>
/// Loads Binance spot market data over HTTP: resources and instruments from the exchange info endpoint, and
/// candle history from the klines endpoint.
/// </summary>
/// <param name="config">The resolved market connection settings.</param>
/// <param name="exchangeInfoRequestFactory">The request factory for the exchange info endpoint.</param>
/// <param name="candleRequestFactory">The request factory for the candle history (klines) endpoint.</param>
/// <param name="rateLimiter">The rate limiter shared across requests made by this provider.</param>
/// <param name="logger">The logger instance.</param>
internal class MarketProvider(
    MarketConfig config,
    IHttpRequestFactory exchangeInfoRequestFactory,
    IHttpRequestFactory candleRequestFactory,
    IRateLimiter rateLimiter,
    ILogger logger
) : MarketProviderBase, IMarketProvider, ILogSubject
{
    /// <summary>Gets the logger instance used by this provider.</summary>
    public ILogger Logger { get; } = logger;

    /// <summary>Loads the current resources and instruments from the exchange info endpoint, and updates the rate limiter's watermark from the reported request weight limit.</summary>
    /// <returns>A result carrying the loaded market context on success.</returns>
    public async Task<MarketResult<MarketContext?>> LoadContextAsync()
    {
        this.Trace("start");

        // load exchange info
        var result = await exchangeInfoRequestFactory
            .New(config.HttpApi)
            .Get("api/v3/exchangeInfo")
            .WithLogFromWithHeaders(this, LogData.Headers)
            .WithRateDelay1M(rateLimiter)
            .AsMarketResultAsync<ExchangeInfo>();

        if (!result.IsSuccess || result.Data is null)
        {
            this.Trace("exchange info load failed");

            return MarketResult.From(result, default(MarketContext));
        }

        this.Trace("resolve resources");
        var resources = ResolveResources(result.Data.Instruments);

        var weightLimit = result.Data.RateLimits.RequestWeightLimit;
        this.Trace("update watermark to {limit}", weightLimit);
        rateLimiter.UpdateLimit(weightLimit);

        this.Trace("done");

        return MarketResult.Ok<MarketContext?>(new MarketContext(resources.Values, result.Data.Instruments));
    }

    /// <summary>Loads 1-minute candles for an instrument over the given time range, paging through the klines endpoint as needed.</summary>
    /// <param name="instrument">The instrument symbol to load candles for.</param>
    /// <param name="start">The start of the time range, inclusive.</param>
    /// <param name="end">The end of the time range, exclusive.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>An asynchronous sequence of candle pages as they are fetched.</returns>
    public async IAsyncEnumerable<MarketResult<IReadOnlyCollection<CandleModel>?>> LoadCandlesAsync(
        string instrument,
        Instant start,
        Instant end,
        [EnumeratorCancellation] CancellationToken ct
    )
    {
        await foreach (var candles in LoadCandlesBaseAsync(instrument, start, end, 1000, FetchAsync, ct))
            yield return candles;

        Task<MarketResult<List<CandleModel>?>> FetchAsync(string symbol, Instant from, int count) =>
            candleRequestFactory
                .New(config.HttpApi)
                .Get("api/v3/klines")
                .Param("symbol", instrument)
                .Param("interval", "1m")
                .Param("limit", count)
                .Param("startTime", from.ToUnixTimeMilliseconds())
                .WithLogFromWithHeaders(this, LogData.Headers)
                .WithRateDelay1M(rateLimiter)
                .AsMarketResultAsync<List<CandleModel>>();
    }
}
