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

namespace Annium.Finance.Providers.Crypto.Binance.Base.Internal.Shared.TimeSync;

/// <summary>
/// Fetches Binance's current server time from its <c>/time</c> market endpoint, used to keep locally computed
/// request timestamps and signatures within the exchange's accepted <c>recvWindow</c>.
/// </summary>
internal class ServerTimeProvider : IServerTimeProvider, ILogSubject
{
    /// <summary>The factory used to build the server time HTTP request.</summary>
    private readonly IHttpRequestFactory _requestFactory;

    /// <summary>The base URI of the market HTTP API to request the server time from.</summary>
    private readonly Uri _httpApi;

    /// <summary>The relative path of the server time endpoint.</summary>
    private readonly string _endpoint;

    /// <summary>Gets the logger used to trace the server time request.</summary>
    public ILogger Logger { get; }

    /// <summary>Initializes a new instance of the <see cref="ServerTimeProvider"/> class.</summary>
    /// <param name="requestFactory">The factory used to build the server time HTTP request.</param>
    /// <param name="httpApi">The base URI of the market HTTP API to request the server time from.</param>
    /// <param name="endpoint">The relative path of the server time endpoint.</param>
    /// <param name="logger">The logger to trace the request with.</param>
    public ServerTimeProvider(IHttpRequestFactory requestFactory, Uri httpApi, string endpoint, ILogger logger)
    {
        _requestFactory = requestFactory;
        _httpApi = httpApi;
        _endpoint = endpoint;
        Logger = logger;
    }

    /// <summary>Requests Binance's current server time.</summary>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A market result carrying the server time in milliseconds since the Unix epoch, or 0 on failure.</returns>
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
