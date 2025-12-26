using System;
using System.Net.Mime;
using Annium.Core.DependencyInjection;
using Annium.Core.Mapper;
using Annium.Finance.Providers.Abstractions.Connectors.User;
using Annium.Finance.Providers.Abstractions.Domain.Shared;
using Annium.Finance.Providers.Abstractions.Domain.User;
using Annium.Finance.Providers.Core.Shared.Loaders;
using Annium.Finance.Providers.Core.Shared.RateLimits;
using Annium.Finance.Providers.Core.Shared.Status;
using Annium.Finance.Providers.Crypto.Binance.Base.User;
using Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.User.Services;
using Annium.Logging;
using Annium.Net.Http;
using Annium.Serialization.Abstractions;
using static Annium.Finance.Providers.Crypto.Binance.UsdFutures.Constants;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.User;

internal class UserConnectorFactory(IServiceProvider sp) : IUserConnectorFactory
{
    public IUserConnector Create(UserSettings settings)
    {
        var config = sp.Resolve<IMapper>().Map<UserConfig>(settings);
        var providerKey = settings.GetProviderKey();

        var queryProcessor = sp.Resolve<QueryProcessor>();
        var signatureService = sp.CreateSignatureService(settings, providerKey);
        var setLeverageRequestFactory = sp.ResolveHttpRequestFactory(SetLeverageKey);
        var initOrderRequestFactory = sp.ResolveHttpRequestFactory(InitOrderKey);
        var modifyOrderRequestFactory = sp.ResolveHttpRequestFactory(ModifyOrderKey);
        var cancelOrderRequestFactory = sp.ResolveHttpRequestFactory(CancelOrderKey);
        var cancelAllOrdersRequestFactory = sp.ResolveHttpRequestFactory(CancelAllOrdersKey);
        var listenKeyResolver = sp.CreateListenKeyResolver(
            config,
            "/fapi/v1/listenKey",
            ListenKeyKey,
            signatureService
        );
        var userStream = sp.CreateUserStream(config, listenKeyResolver);
        var orderUpdateEventSerializer = sp.ResolveSerializer<ReadOnlyMemory<byte>>(
            OrderUpdateKey,
            MediaTypeNames.Application.Json
        );
        var loaderFactory = sp.Resolve<ILoaderFactory>();
        var providerFactory = sp.ResolveKeyed<IUserProviderFactory>(config.Provider);
        var provider = providerFactory.Create(settings);
        var rateLimiter = sp.Resolve<IRateLimiter>();
        var reporter = sp.Resolve<IStatusReporter>();
        var monitor = sp.Resolve<IStatusMonitor>();
        var logger = sp.Resolve<ILogger>();

        return new UserConnector(
            config,
            queryProcessor,
            signatureService,
            setLeverageRequestFactory,
            initOrderRequestFactory,
            modifyOrderRequestFactory,
            cancelOrderRequestFactory,
            cancelAllOrdersRequestFactory,
            rateLimiter,
            loaderFactory,
            userStream,
            orderUpdateEventSerializer,
            provider,
            reporter,
            monitor,
            logger
        );
    }
}
