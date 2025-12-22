using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Connectors.Market;
using Annium.Finance.Providers.Abstractions.Domain.Market;
using Annium.Finance.Providers.Abstractions.Domain.Market.Operations;
using Annium.Finance.Providers.Abstractions.Domain.Shared;
using Annium.Finance.Providers.Core.Market;
using Annium.Finance.Providers.Core.Shared.RateLimits;
using Annium.Finance.Providers.Crypto.Binance.Base.Shared.HttpExtensions;
using Annium.Finance.Providers.Crypto.Binance.Spot.Internal.Market.Contracts.Domain;
using Annium.Finance.Providers.Crypto.Binance.Spot.Internal.Market.HttpExtensions;
using Annium.Finance.Providers.Crypto.Binance.Spot.Internal.Shared;
using Annium.Logging;
using Annium.Net.Http;
using NodaTime;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Internal.Market;

internal class MarketProvider(
    ProviderEnvironment env,
    IHttpRequestFactory exchangeInfoRequestFactory,
    IHttpRequestFactory candleRequestFactory,
    IRateLimiter rateLimiter,
    ILogger logger
) : MarketProviderBase, IMarketProvider, ILogSubject
{
    public ILogger Logger { get; } = logger;

    public async Task<MarketResult<MarketContext?>> LoadContextAsync()
    {
        this.Trace("start");

        // load exchange info
        var result = await exchangeInfoRequestFactory
            .New(Endpoints.GetHttpApi(env))
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
                .New(Endpoints.GetHttpApi(env))
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
