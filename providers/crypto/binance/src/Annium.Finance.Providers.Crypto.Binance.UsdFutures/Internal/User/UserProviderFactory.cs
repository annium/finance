using System;
using Annium.Core.DependencyInjection;
using Annium.Core.Mapper;
using Annium.Finance.Providers.Abstractions.Connectors.User;
using Annium.Finance.Providers.Abstractions.Domain.Shared;
using Annium.Finance.Providers.Abstractions.Domain.User;
using Annium.Finance.Providers.Core.Shared.RateLimits;
using Annium.Finance.Providers.Crypto.Binance.Base.User;
using Annium.Logging;
using Annium.Net.Http;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.User;

internal class UserProviderFactory(IServiceProvider sp) : IUserProviderFactory
{
    public IUserProvider Create(UserSettings settings)
    {
        var providerKey = settings.GetProviderKey();
        var config = sp.Resolve<IMapper>().Map<UserConfig>(settings);

        var timeProvider = sp.Resolve<ITimeProvider>();
        var signatureService = sp.CreateSignatureService(settings, providerKey);
        var getAccountRequestFactory = sp.ResolveHttpRequestFactory(Constants.GetAccountKey);
        var getOrderRequestFactory = sp.ResolveHttpRequestFactory(Constants.GetOrderKey);
        var getTradeRequestFactory = sp.ResolveHttpRequestFactory(Constants.GetTradeKey);
        var rateLimiter = sp.Resolve<IRateLimiter>();
        var logger = sp.Resolve<ILogger>();

        return new UserProvider(
            config,
            timeProvider,
            signatureService,
            getAccountRequestFactory,
            getOrderRequestFactory,
            getTradeRequestFactory,
            rateLimiter,
            logger
        );
    }
}
