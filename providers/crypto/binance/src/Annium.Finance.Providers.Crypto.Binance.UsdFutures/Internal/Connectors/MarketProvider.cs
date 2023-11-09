using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Annium.Core.DependencyInjection;
using Annium.Finance.Providers.Abstractions.Connectors.Connectors;
using Annium.Finance.Providers.Abstractions.Domain.Dto;
using Annium.Finance.Providers.Abstractions.Domain.Enums;
using Annium.Finance.Providers.Abstractions.Domain.Operations;
using Annium.Finance.Providers.Crypto.Binance.Base.Connectors.Extensions;
using Annium.Finance.Providers.Crypto.Binance.Base.Contracts.Market.Domain;
using Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Connectors.Extensions;
using Annium.Finance.Providers.Shared.Connectors;
using Annium.Finance.Providers.Shared.Internal.Connectors;
using Annium.Logging;
using Annium.Net.Http;
using NodaTime;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Connectors;

internal class MarketProvider : MarketProviderBase, IMarketProvider, ILogSubject
{
    public ILogger Logger { get; }
    private readonly IHttpRequestFactory _exchangeInfoRequestFactory;
    private readonly IHttpRequestFactory _candleRequestFactory;

    public MarketProvider(IIndex<string, IHttpRequestFactory> httpRequestFactories, ILogger logger)
    {
        Logger = logger;
        _exchangeInfoRequestFactory = httpRequestFactories[Constants.ExchangeInfoKey];
        _candleRequestFactory = httpRequestFactories[Constants.CandleKey];
    }

    public async Task<MarketResult<MarketContext>> LoadContextAsync(ProviderEnvironment env)
    {
        this.Trace("start");

        // load exchange info
        var result = await _exchangeInfoRequestFactory
            .New(Endpoints.GetHttpApiEndpoint(env))
            .Get("fapi/v1/exchangeInfo")
            .WithLogFrom(this)
            .WithRateDelay1M()
            .AsMarketResultAsync<ExchangeInfo?>(null);

        if (result.IsFailure)
        {
            this.Trace("exchange info load failed");

            return MarketResult.From(
                result,
                new MarketContext(Array.Empty<ResourceDto>(), Array.Empty<InstrumentDto>())
            );
        }

        this.Trace("resolve resources");
        var resources = ResolveResources(result.Data.Instruments);

        this.Trace("update watermark (can change over time");
        HttpRequestRateExtensions.UpdateRequestWeightLimit(result.Data.RateLimits.RequestWeightLimit);

        this.Trace("done");

        return MarketResult.Ok(new MarketContext(resources, result.Data.Instruments));
    }

    public async IAsyncEnumerable<MarketResult<IReadOnlyCollection<CandleDto>>> LoadCandlesAsync(
        string instrument,
        ProviderEnvironment env,
        Instant start,
        Instant end,
        [EnumeratorCancellation] CancellationToken ct
    )
    {
        await foreach (var candles in LoadCandlesBaseAsync(instrument, start, end, 1000, Fetch, ct))
            yield return candles;

        Task<MarketResult<List<CandleDto>>> Fetch(string symbol, Instant from, int count) =>
            _candleRequestFactory
                .New(Endpoints.GetHttpApiEndpoint(env))
                .Get("fapi/v1/klines")
                .Param("symbol", instrument)
                .Param("interval", "1m")
                .Param("limit", count)
                .Param("startTime", from.ToUnixTimeMilliseconds())
                .WithLogFrom(this)
                .WithRateDelay1M()
                .AsMarketResultAsync(new List<CandleDto>());
    }
}
