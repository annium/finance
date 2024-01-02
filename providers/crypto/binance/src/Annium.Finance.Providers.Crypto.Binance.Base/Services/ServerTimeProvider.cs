using System.Threading;
using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Domain.Operations;
using Annium.Finance.Providers.Crypto.Binance.Base.Connectors.Extensions;
using Annium.Finance.Providers.Crypto.Binance.Base.Contracts.Shared.Domain;
using Annium.Finance.Providers.Shared.Connectors;
using Annium.Finance.Providers.Shared.Services;
using Annium.Logging;
using Annium.Net.Http;

namespace Annium.Finance.Providers.Crypto.Binance.Base.Services;

public class ServerTimeProvider : ServerTimeProviderBase
{
    private readonly MarketConfigBase _config;
    private readonly IHttpRequestFactory _requestFactory;

    public ServerTimeProvider(
        MarketConfigBase config,
        IHttpRequestFactory requestFactory,
        ServerTimeWatcherConfig watcherConfig,
        IStatusReporter statusReporter,
        ILogger logger
    )
        : base(watcherConfig, statusReporter, logger)
    {
        _config = config;
        _requestFactory = requestFactory;
    }

    protected override async Task<MarketResult<long>> LoadAsync(CancellationToken ct)
    {
        // load exchange info
        var result = await _requestFactory
            .New(_config.HttpApi)
            .Get(_config.ServerTimeEndpoint)
            .WithLogFrom(this, LogData.Headers | LogData.Response)
            .AsMarketResultAsync<ServerTime>();

        return MarketResult.From(result, result.IsSuccess ? result.Data.Value : 0L);
    }
}
