using Annium.Core.Mapper;
using Annium.Finance.Providers.Abstractions.Domain.Market;
using Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Shared;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Market.Profiles;

/// <summary>
/// Mapper profile that resolves a <see cref="MarketConfig"/> (endpoints, websocket path) from the generic
/// <see cref="MarketSettings"/> for the USD-M futures provider.
/// </summary>
internal class MarketConfigProfile : Profile
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MarketConfigProfile"/> class, registering the
    /// <see cref="MarketSettings"/> to <see cref="MarketConfig"/> mapping.
    /// </summary>
    public MarketConfigProfile()
    {
        Map<MarketSettings, MarketConfig>(x => MapSettingsToConfig(x));
    }

    /// <summary>
    /// Resolves the HTTP and websocket API endpoints for the settings' environment and fills in the fixed
    /// USD-M futures websocket stream path.
    /// </summary>
    /// <param name="settings">The generic market settings to map from.</param>
    /// <returns>The resolved market configuration.</returns>
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
