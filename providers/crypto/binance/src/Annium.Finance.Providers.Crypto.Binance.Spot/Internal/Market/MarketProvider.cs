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
using Microsoft.Extensions.DependencyInjection;
using NodaTime;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Internal.Market;

internal class MarketProvider : MarketProviderBase, IMarketProvider, ILogSubject
{
    public ILogger Logger { get; }
    private readonly IHttpRequestFactory _exchangeInfoRequestFactory;
    private readonly IHttpRequestFactory _candleRequestFactory;
    private readonly IRateLimiter _rateLimiter;

    public MarketProvider(
        [FromKeyedServices(Constants.ExchangeInfoKey)] IHttpRequestFactory exchangeInfoRequestFactory,
        [FromKeyedServices(Constants.CandleKey)] IHttpRequestFactory candleRequestFactory,
        IRateLimiter rateLimiter,
        ILogger logger
    )
    {
        Logger = logger;
        _exchangeInfoRequestFactory = exchangeInfoRequestFactory;
        _candleRequestFactory = candleRequestFactory;
        _rateLimiter = rateLimiter;
    }

    public async Task<MarketResult<MarketContext?>> LoadContextAsync(ProviderEnvironment env)
    {
        this.Trace("start");

        // load exchange info
        var result = await _exchangeInfoRequestFactory
            .New(Endpoints.GetHttpApi(env))
            .Get("api/v3/exchangeInfo")
            .WithLogFromWithHeaders(this, LogData.Headers)
            .WithRateDelay1M(_rateLimiter)
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
        _rateLimiter.UpdateLimit(weightLimit);

        this.Trace("done");

        return MarketResult.Ok<MarketContext?>(new MarketContext(resources.Values, result.Data.Instruments));
    }

    public async IAsyncEnumerable<MarketResult<IReadOnlyCollection<CandleModel>?>> LoadCandlesAsync(
        string instrument,
        ProviderEnvironment env,
        Instant start,
        Instant end,
        [EnumeratorCancellation] CancellationToken ct
    )
    {
        await foreach (var candles in LoadCandlesBaseAsync(instrument, start, end, 1000, FetchAsync, ct))
            yield return candles;

        Task<MarketResult<List<CandleModel>?>> FetchAsync(string symbol, Instant from, int count) =>
            _candleRequestFactory
                .New(Endpoints.GetHttpApi(env))
                .Get("api/v3/klines")
                .Param("symbol", instrument)
                .Param("interval", "1m")
                .Param("limit", count)
                .Param("startTime", from.ToUnixTimeMilliseconds())
                .WithLogFromWithHeaders(this, LogData.Headers)
                .WithRateDelay1M(_rateLimiter)
                .AsMarketResultAsync<List<CandleModel>>();
    }
}
