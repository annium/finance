using System;
using Annium.Finance.Providers.Abstractions.Domain.Enums;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal;

internal static class Endpoints
{
    public static string GetHttpApiEndpoint(ProviderEnvironment env) =>
        env switch
        {
            ProviderEnvironment.Real => "https://fapi.binance.com",
            ProviderEnvironment.Test => "https://testnet.binancefuture.com",
            _ => throw new ArgumentException($"Unsupported {env} environment")
        };

    public static string GetWsApiEndpoint(ProviderEnvironment env) =>
        env switch
        {
            ProviderEnvironment.Real => "wss://fstream.binance.com",
            ProviderEnvironment.Test => "wss://stream.binancefuture.com",
            _ => throw new ArgumentException($"Unsupported {env} environment")
        };
}
