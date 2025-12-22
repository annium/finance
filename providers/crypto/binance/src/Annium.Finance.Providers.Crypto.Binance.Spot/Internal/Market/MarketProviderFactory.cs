using System;
using Annium.Core.DependencyInjection;
using Annium.Finance.Providers.Abstractions.Connectors.Market;
using Annium.Finance.Providers.Abstractions.Domain.Shared;
using Annium.Finance.Providers.Core.Shared.RateLimits;
using Annium.Logging;
using Annium.Net.Http;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Internal.Market;

internal class MarketProviderFactory(IServiceProvider sp) : IMarketProviderFactory
{
    public IMarketProvider Create(ProviderEnvironment env)
    {
        var exchangeInfoRequestFactory = sp.ResolveHttpRequestFactory(Constants.ExchangeInfoKey);
        var candleRequestFactory = sp.ResolveHttpRequestFactory(Constants.CandleKey);
        var rateLimiter = sp.Resolve<IRateLimiter>();
        var logger = sp.Resolve<ILogger>();

        return new MarketProvider(env, exchangeInfoRequestFactory, candleRequestFactory, rateLimiter, logger);
    }
}
