using System;
using Annium.Core.DependencyInjection;
using Annium.Core.Mapper;
using Annium.Finance.Providers.Abstractions.Connectors.Market;
using Annium.Finance.Providers.Abstractions.Domain.Market;
using Annium.Finance.Providers.Core.Shared.RateLimits;
using Annium.Logging;
using Annium.Net.Http;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Internal.Market;

/// <summary>Builds Binance spot <see cref="MarketProvider"/> instances, resolving their dependencies from the container.</summary>
/// <param name="sp">The service provider used to resolve dependencies.</param>
internal class MarketProviderFactory(IServiceProvider sp) : IMarketProviderFactory
{
    /// <summary>Creates a new Binance spot market provider for the given settings.</summary>
    /// <param name="settings">The market connection settings to configure the provider with.</param>
    /// <returns>The created market provider.</returns>
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
