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
using Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Market.Contracts.Domain;
using Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Market.HttpExtensions;
using Annium.Logging;
using Annium.Net.Http;
using NodaTime;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Market;

/// <summary>
/// Loads USD-M futures market data straight from the Binance REST API: exchange info (resources and instruments)
/// and historical klines/candles.
/// </summary>
/// <param name="config">The resolved market configuration.</param>
/// <param name="exchangeInfoRequestFactory">Factory for requests against the exchange info endpoint.</param>
/// <param name="candleRequestFactory">Factory for requests against the klines/candles endpoint.</param>
/// <param name="rateLimiter">Limits request weight against the exchange's rate limits.</param>
/// <param name="logger">The logger.</param>
internal class MarketProvider(
    MarketConfig config,
    IHttpRequestFactory exchangeInfoRequestFactory,
    IHttpRequestFactory candleRequestFactory,
    IRateLimiter rateLimiter,
    ILogger logger
) : MarketProviderBase, IMarketProvider, ILogSubject
{
    /// <summary>Gets the logger for this provider.</summary>
    public ILogger Logger { get; } = logger;

    /// <summary>
    /// Loads exchange info and derives the resource (asset) and instrument sets, updating the rate limiter's
    /// weight limit from the reported <c>REQUEST_WEIGHT</c> rate limit.
    /// </summary>
    /// <returns>A result carrying the resolved market context, or a failure status if exchange info could not be loaded.</returns>
    public async Task<MarketResult<MarketContext?>> LoadContextAsync()
    {
        this.Trace("start");

        // load exchange info
        var result = await exchangeInfoRequestFactory
            .New(config.HttpApi)
            .Get("fapi/v1/exchangeInfo")
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
        foreach (var asset in result.Data.Assets)
            if (!resources.ContainsKey(asset.Code))
                resources[asset.Code] = new ResourceModel(asset.Code, (byte)(asset.Code.Contains("USD") ? 2 : 8));

        var weightLimit = result.Data.RateLimits.RequestWeightLimit;
        this.Trace("update watermark to {limit}", weightLimit);
        rateLimiter.UpdateLimit(weightLimit);

        this.Trace("done");

        return MarketResult.Ok<MarketContext?>(new MarketContext(resources.Values, result.Data.Instruments));
    }

    /// <summary>
    /// Loads 1-minute klines/candles for the given instrument and time range, paging through the exchange's
    /// 1000-candle-per-request limit.
    /// </summary>
    /// <param name="instrument">The instrument symbol to load candles for.</param>
    /// <param name="start">The inclusive start of the time range.</param>
    /// <param name="end">The exclusive end of the time range.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>An asynchronous sequence of candle page results, in chronological order.</returns>
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
                .Get("fapi/v1/klines")
                .Param("symbol", instrument)
                .Param("interval", "1m")
                .Param("limit", count)
                .Param("startTime", from.ToUnixTimeMilliseconds())
                .WithLogFromWithHeaders(this, LogData.Headers)
                .WithRateDelay1M(rateLimiter)
                .AsMarketResultAsync<List<CandleModel>>();
    }
}
