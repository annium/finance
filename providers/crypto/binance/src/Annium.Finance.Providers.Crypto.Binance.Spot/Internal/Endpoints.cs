using System;
using Annium.Finance.Providers.Abstractions.Domain.Enums;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Internal;

internal static class Endpoints
{
    public static string GetHttpApiEndpoint(ProviderEnvironment env) =>
        env switch
        {
            ProviderEnvironment.Real => "https://api.binance.com",
            ProviderEnvironment.Test => "https://testnet.binance.vision",
            _ => throw new ArgumentException($"Unsupported {env} environment")
        };

    public static string GetWsApiEndpoint(ProviderEnvironment env) =>
        env switch
        {
            ProviderEnvironment.Real => "wss://stream.binance.com:9443",
            ProviderEnvironment.Test => "wss://testnet.binance.vision",
            _ => throw new ArgumentException($"Unsupported {env} environment")
        };
}
