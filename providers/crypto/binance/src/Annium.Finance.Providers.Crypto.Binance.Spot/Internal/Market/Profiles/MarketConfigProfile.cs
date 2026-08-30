using Annium.Core.Mapper;
using Annium.Finance.Providers.Abstractions.Domain.Market;
using Annium.Finance.Providers.Crypto.Binance.Spot.Internal.Shared;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Internal.Market.Profiles;

/// <summary>Mapper profile that resolves a <see cref="MarketConfig"/> from generic <see cref="MarketSettings"/>.</summary>
internal class MarketConfigProfile : Profile
{
    /// <summary>Initializes a new instance of the <see cref="MarketConfigProfile"/> class, registering the settings-to-config mapping.</summary>
    public MarketConfigProfile()
    {
        Map<MarketSettings, MarketConfig>(x => MapSettingsToConfig(x));
    }

    /// <summary>Maps generic market settings into a <see cref="MarketConfig"/>, resolving the Binance HTTP and WebSocket endpoints for the target environment.</summary>
    /// <param name="settings">The generic market settings to map from.</param>
    /// <returns>The resolved market config.</returns>
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
