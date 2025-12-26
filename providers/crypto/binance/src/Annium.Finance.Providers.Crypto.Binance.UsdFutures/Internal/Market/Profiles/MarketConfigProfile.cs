using Annium.Core.Mapper;
using Annium.Finance.Providers.Abstractions.Domain.Market;
using Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Shared;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Market.Profiles;

internal class MarketConfigProfile : Profile
{
    public MarketConfigProfile()
    {
        Map<MarketSettings, MarketConfig>(x => MapSettingsToConfig(x));
    }

    private static MarketConfig MapSettingsToConfig(MarketSettings settings)
    {
        var httpApi = Endpoints.GetHttpApi(settings.Environment);
        var wsApi = Endpoints.GetWsApi(settings.Environment);

        return new MarketConfig
        {
            Provider = settings.Provider,
            Environment = settings.Environment,
            HttpApi = httpApi,
            WsApi = wsApi,
            WsUriPath = "/stream",
        };
    }
}
