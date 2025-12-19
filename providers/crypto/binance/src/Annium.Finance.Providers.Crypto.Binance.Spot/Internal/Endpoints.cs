using System;
using Annium.Finance.Providers.Abstractions.Domain.Shared;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Internal;

internal static class Endpoints
{
    public static Uri GetHttpApi(ProviderEnvironment env) =>
        env switch
        {
            ProviderEnvironment.Real => new Uri("https://api.binance.com"),
            ProviderEnvironment.Test => new Uri("https://testnet.binance.vision"),
            _ => throw new ArgumentException($"Unsupported {env} environment"),
        };

    public static Uri GetWsApi(ProviderEnvironment env) =>
        env switch
        {
            ProviderEnvironment.Real => new Uri("wss://stream.binance.com"),
            ProviderEnvironment.Test => new Uri("wss://testnet.binance.vision"),
            _ => throw new ArgumentException($"Unsupported {env} environment"),
        };
}
