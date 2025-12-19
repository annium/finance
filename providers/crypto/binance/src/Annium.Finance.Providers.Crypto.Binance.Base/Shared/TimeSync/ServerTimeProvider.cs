using System;
using System.Threading;
using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Domain.Market.Operations;
using Annium.Finance.Providers.Core.Shared.TimeSync;
using Annium.Finance.Providers.Crypto.Binance.Base.Internal.Market.HttpExtensions;
using Annium.Finance.Providers.Crypto.Binance.Base.Shared.Contracts.Domain;
using Annium.Finance.Providers.Crypto.Binance.Base.Shared.HttpExtensions;
using Annium.Logging;
using Annium.Net.Http;

namespace Annium.Finance.Providers.Crypto.Binance.Base.Shared.TimeSync;

public class ServerTimeProvider : IServerTimeProvider, ILogSubject
{
    private readonly IHttpRequestFactory _requestFactory;
    private readonly Uri _httpApi;
    private readonly string _endpoint;
    public ILogger Logger { get; }

    public ServerTimeProvider(IHttpRequestFactory requestFactory, Uri httpApi, string endpoint, ILogger logger)
    {
        _requestFactory = requestFactory;
        _httpApi = httpApi;
        _endpoint = endpoint;
        Logger = logger;
    }

    public async Task<MarketResult<long>> LoadAsync(CancellationToken ct)
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
