using System;
using Annium.Core.DependencyInjection;
using Annium.Finance.Providers.Abstractions.Domain.Shared;
using Annium.Finance.Providers.Abstractions.Domain.User;
using Annium.Finance.Providers.Core.Shared.Status;
using Annium.Finance.Providers.Core.Shared.TimeSync;
using Annium.Finance.Providers.Crypto.Binance.Base.Internal.User.Services;
using Annium.Finance.Providers.Crypto.Binance.Base.User.Services;
using Annium.Logging;
using Annium.Net.Http;

namespace Annium.Finance.Providers.Crypto.Binance.Base.User;

/// <summary>Factory extension methods for constructing Binance account/trading services from an <see cref="IServiceProvider"/>.</summary>
public static class ServiceProviderExtensions
{
    /// <summary>Creates a <see cref="SignatureService"/> that signs requests with the given account's key and secret, using the keyed server time source.</summary>
    /// <param name="sp">The service provider to resolve dependencies from.</param>
    /// <param name="settings">The user settings providing the API key and secret.</param>
    /// <param name="providerKey">The key identifying the registered <see cref="IServerTimeSource"/> to resolve.</param>
    /// <returns>The created signature service.</returns>
    public static ISignatureService CreateSignatureService(
        this IServiceProvider sp,
        UserSettings settings,
        ProviderKey providerKey
    )
    {
        var serverTimeSource = sp.ResolveKeyed<IServerTimeSource>(providerKey);

        return new SignatureService(settings, serverTimeSource);
    }

    /// <summary>Creates a <see cref="UserStream"/> that connects to the user data stream WebSocket using keys supplied by the given listen key resolver.</summary>
    /// <param name="sp">The service provider to resolve dependencies from.</param>
    /// <param name="config">The user configuration providing the WebSocket API and listen key URI path.</param>
    /// <param name="listenKeyResolver">The resolver supplying and refreshing the listen key the stream connects with.</param>
    /// <returns>The created user stream.</returns>
    public static IUserStream CreateUserStream(
        this IServiceProvider sp,
        UserConfigBase config,
        IListenKeyResolver listenKeyResolver
    )
    {
        var statusReporter = sp.Resolve<IStatusReporter>();
        var logger = sp.Resolve<ILogger>();

        return new UserStream(config, listenKeyResolver, statusReporter, logger);
    }

    /// <summary>Creates a <see cref="ListenKeyResolver"/> that fetches and keeps alive a listen key from the given endpoint.</summary>
    /// <param name="sp">The service provider to resolve dependencies from.</param>
    /// <param name="config">The user configuration providing the HTTP API and listen key fetch/confirm intervals.</param>
    /// <param name="endpoint">The relative path of the listen key endpoint.</param>
    /// <param name="listenKeyKey">The keyed HTTP request factory registration key to resolve the request factory with.</param>
    /// <param name="signatureService">The service used to sign the listen key request.</param>
    /// <returns>The created listen key resolver.</returns>
    public static IListenKeyResolver CreateListenKeyResolver(
        this IServiceProvider sp,
        UserConfigBase config,
        string endpoint,
        string listenKeyKey,
        ISignatureService signatureService
    )
    {
        var httpRequestFactory = sp.ResolveHttpRequestFactory(listenKeyKey);
        var statusReporter = sp.Resolve<IStatusReporter>();
        var logger = sp.Resolve<ILogger>();

        return new ListenKeyResolver(config, endpoint, httpRequestFactory, signatureService, statusReporter, logger);
    }
}
