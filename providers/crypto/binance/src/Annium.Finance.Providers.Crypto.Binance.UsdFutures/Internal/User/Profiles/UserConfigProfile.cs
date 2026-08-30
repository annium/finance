using System;
using Annium.Core.DependencyInjection;
using Annium.Core.Mapper;
using Annium.Finance.Providers.Abstractions.Domain.User;
using Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Shared;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.User.Profiles;

/// <summary>
/// Mapper profile that resolves a <see cref="UserConfig"/> (endpoints, credentials, reload behavior) from the
/// generic <see cref="UserSettings"/> for the USD-M futures provider.
/// </summary>
internal class UserConfigProfile : Profile
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserConfigProfile"/> class, registering the
    /// <see cref="UserSettings"/> to <see cref="UserConfig"/> mapping.
    /// </summary>
    /// <param name="sp">The service provider used to resolve the shared <see cref="ProviderConfiguration"/>.</param>
    public UserConfigProfile(IServiceProvider sp)
    {
        Map<UserSettings, UserConfig>(x => MapSettingsToConfig(sp, x));
    }

    /// <summary>
    /// Resolves the HTTP and websocket API endpoints for the settings' environment, fills in the fixed listen
    /// key websocket path, and copies the listen-key and reload behavior from the shared
    /// <see cref="ProviderConfiguration"/>.
    /// </summary>
    /// <param name="sp">The service provider used to resolve the shared <see cref="ProviderConfiguration"/>.</param>
    /// <param name="settings">The generic user settings to map from.</param>
    /// <returns>The resolved user configuration.</returns>
    private static UserConfig MapSettingsToConfig(IServiceProvider sp, UserSettings settings)
    {
        var httpApi = Endpoints.GetHttpApi(settings.Environment);
        var wsApi = Endpoints.GetWsApi(settings.Environment);

        var providerConfig = sp.Resolve<ProviderConfiguration>();

        return new UserConfig
        {
            Provider = settings.Provider,
            Environment = settings.Environment,
            Key = settings.Key,
            Secret = settings.Secret,
            HttpApi = httpApi,
            WsApi = wsApi,
            ListenKeyUriPath = "/ws/",
            ListenKey = providerConfig.ListenKey,
            ReloadContext = providerConfig.ReloadContext,
            ReloadOrders = providerConfig.ReloadOrders,
            ReloadTrades = providerConfig.ReloadTrades,
        };
    }
}
