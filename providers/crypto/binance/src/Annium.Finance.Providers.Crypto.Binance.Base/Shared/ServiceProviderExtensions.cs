using System;
using Annium.Core.DependencyInjection;
using Annium.Finance.Providers.Core.Shared.TimeSync;
using Annium.Finance.Providers.Crypto.Binance.Base.Internal.Shared.TimeSync;
using Annium.Logging;
using Annium.Net.Http;

namespace Annium.Finance.Providers.Crypto.Binance.Base.Shared;

/// <summary>Factory extension methods for constructing the Binance server time provider from an <see cref="IServiceProvider"/>.</summary>
public static class ServiceProviderExtensions
{
    /// <summary>Creates a <see cref="ServerTimeProvider"/> that fetches Binance's server time from the given endpoint.</summary>
    /// <param name="sp">The service provider to resolve dependencies from.</param>
    /// <param name="serverTimeKey">The keyed HTTP request factory registration key to resolve the request factory with.</param>
    /// <param name="httpApi">The base URI of the market HTTP API to request the server time from.</param>
    /// <param name="endpoint">The relative path of the server time endpoint.</param>
    /// <returns>The created server time provider.</returns>
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
