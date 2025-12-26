using System;
using Annium.Core.DependencyInjection;
using Annium.Finance.Providers.Abstractions.Domain.Shared;
using Annium.Finance.Providers.Abstractions.Domain.User;
using Annium.Finance.Providers.Core.Shared.Status;
using Annium.Finance.Providers.Core.Shared.TimeSync;
using Annium.Finance.Providers.Crypto.Binance.Base.User.Services;
using Annium.Logging;
using Annium.Net.Http;

namespace Annium.Finance.Providers.Crypto.Binance.Base.User;

public static class ServiceProviderExtensions
{
    public static SignatureService CreateSignatureService(
        this IServiceProvider sp,
        UserSettings settings,
        ProviderKey providerKey
    )
    {
        var serverTimeSource = sp.ResolveKeyed<IServerTimeSource>(providerKey);

        return new SignatureService(settings, serverTimeSource);
    }

    public static UserStream CreateUserStream(
        this IServiceProvider sp,
        UserConfigBase config,
        ListenKeyResolver listenKeyResolver
    )
    {
        var statusReporter = sp.Resolve<IStatusReporter>();
        var logger = sp.Resolve<ILogger>();

        return new UserStream(config, listenKeyResolver, statusReporter, logger);
    }

    public static ListenKeyResolver CreateListenKeyResolver(
        this IServiceProvider sp,
        UserConfigBase config,
        string endpoint,
        string listenKeyKey,
        SignatureService signatureService
    )
    {
        var httpRequestFactory = sp.ResolveHttpRequestFactory(listenKeyKey);
        var statusReporter = sp.Resolve<IStatusReporter>();
        var logger = sp.Resolve<ILogger>();

        return new ListenKeyResolver(config, endpoint, httpRequestFactory, signatureService, statusReporter, logger);
    }
}
