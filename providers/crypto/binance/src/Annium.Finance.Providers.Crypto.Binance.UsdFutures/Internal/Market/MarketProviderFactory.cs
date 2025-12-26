using System;
using Annium.Core.DependencyInjection;
using Annium.Core.Mapper;
using Annium.Finance.Providers.Abstractions.Connectors.Market;
using Annium.Finance.Providers.Abstractions.Domain.Market;
using Annium.Finance.Providers.Core.Shared.RateLimits;
using Annium.Logging;
using Annium.Net.Http;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Market;

internal class MarketProviderFactory(IServiceProvider sp) : IMarketProviderFactory
{
    public IMarketProvider Create(MarketSettings settings)
    {
        var config = sp.Resolve<IMapper>().Map<MarketConfig>(settings);

        var exchangeInfoRequestFactory = sp.ResolveHttpRequestFactory(Constants.ExchangeInfoKey);
        var candleRequestFactory = sp.ResolveHttpRequestFactory(Constants.CandleKey);
        var rateLimiter = sp.Resolve<IRateLimiter>();
        var logger = sp.Resolve<ILogger>();

        return new MarketProvider(config, exchangeInfoRequestFactory, candleRequestFactory, rateLimiter, logger);
    }
}
