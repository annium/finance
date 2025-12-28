using System;
using Annium.Core.DependencyInjection;
using Annium.Finance.Providers.Core.Shared.TimeSync;
using Annium.Finance.Providers.Crypto.Binance.Base.Internal.Shared.TimeSync;
using Annium.Logging;
using Annium.Net.Http;

namespace Annium.Finance.Providers.Crypto.Binance.Base.Shared;

public static class ServiceProviderExtensions
{
    public static IServerTimeProvider CreateServerTimeProvider(
        this IServiceProvider sp,
        string serverTimeKey,
        Uri httpApi,
        string endpoint
    )
    {
        var requestFactory = sp.ResolveHttpRequestFactory(serverTimeKey);
        var logger = sp.Resolve<ILogger>();

        return new ServerTimeProvider(requestFactory, httpApi, endpoint, logger);
    }
}
