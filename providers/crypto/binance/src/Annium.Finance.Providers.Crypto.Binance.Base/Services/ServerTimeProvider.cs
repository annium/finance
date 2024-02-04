using System;
using System.Threading;
using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Domain.Operations;
using Annium.Finance.Providers.Crypto.Binance.Base.Connectors.Extensions;
using Annium.Finance.Providers.Crypto.Binance.Base.Contracts.Shared.Domain;
using Annium.Finance.Providers.Shared.ServerTime;
using Annium.Logging;
using Annium.Net.Http;

namespace Annium.Finance.Providers.Crypto.Binance.Base.Services;

public class ServerTimeProvider : ServerTimeProviderBase
{
    private readonly IHttpRequestFactory _requestFactory;
    private readonly Uri _httpApi;
    private readonly string _endpoint;

    public ServerTimeProvider(
        IHttpRequestFactory requestFactory,
        Uri httpApi,
        string endpoint,
        ServerTimeProviderConfig providerConfig,
        ILogger logger
    )
        : base(providerConfig, logger)
    {
        _requestFactory = requestFactory;
        _httpApi = httpApi;
        _endpoint = endpoint;
    }

    protected override async Task<MarketResult<long>> LoadAsync(CancellationToken ct)
    {
        // load exchange info
        var result = await _requestFactory
            .New(_httpApi)
            .Get(_endpoint)
            .WithLogFromWithHeaders(this, LogData.Headers | LogData.Response)
            .AsMarketResultAsync<ServerTime>();

        return MarketResult.From(result, result.IsSuccess ? result.Data.Value : 0L);
    }
}
