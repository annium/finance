using System;
using Annium.Core.DependencyInjection;
using Annium.Core.Mapper;
using Annium.Finance.Providers.Abstractions.Domain.User;
using Annium.Finance.Providers.Crypto.Binance.Spot.Internal.Shared;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Internal.User.Profiles;

/// <summary>Mapper profile that resolves a <see cref="UserConfig"/> from generic <see cref="UserSettings"/>.</summary>
internal class UserConfigProfile : Profile
{
    /// <summary>Initializes a new instance of the <see cref="UserConfigProfile"/> class, registering the settings-to-config mapping.</summary>
    /// <param name="sp">The service provider used to resolve the shared <see cref="ProviderConfiguration"/>.</param>
    public UserConfigProfile(IServiceProvider sp)
    {
        Map<UserSettings, UserConfig>(x => MapSettingsToConfig(sp, x));
    }

    /// <summary>Maps generic user settings into a <see cref="UserConfig"/>, resolving the Binance endpoints and the shared reload/listen-key configuration for the target environment.</summary>
    /// <param name="sp">The service provider used to resolve the shared <see cref="ProviderConfiguration"/>.</param>
    /// <param name="settings">The generic user settings to map from.</param>
    /// <returns>The resolved user config.</returns>
    private static UserConfig MapSettingsToConfig(IServiceProvider sp, UserSettings settings)
    {
        var httpApi = Endpoints.HttpApi;
        var wsApi = Endpoints.WsApi;

        var providerConfig = sp.Resolve<ProviderConfiguration>();

        return new UserConfig
        {
            Provider = settings.Provider,
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
