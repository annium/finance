using System;
using Annium.Core.DependencyInjection;
using Annium.Core.Mapper;
using Annium.Finance.Providers.Abstractions.Domain.User;
using Annium.Finance.Providers.Crypto.Binance.Spot.Internal.Shared;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Internal.User.Profiles;

internal class UserConfigProfile : Profile
{
    public UserConfigProfile(IServiceProvider sp)
    {
        Map<UserSettings, UserConfig>(x => MapSettingsToConfig(sp, x));
    }

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
