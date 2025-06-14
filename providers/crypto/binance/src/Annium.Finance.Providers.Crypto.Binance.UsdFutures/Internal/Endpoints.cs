using System;
using Annium.Finance.Providers.Abstractions.Domain.Enums;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal;

internal static class Endpoints
{
    public static Uri GetHttpApi(ProviderEnvironment env) =>
        env switch
        {
            ProviderEnvironment.Real => new Uri("https://fapi.binance.com"),
            ProviderEnvironment.Test => new Uri("https://testnet.binancefuture.com"),
            _ => throw new ArgumentException($"Unsupported {env} environment"),
        };

    public static Uri GetWsApi(ProviderEnvironment env) =>
        env switch
        {
            ProviderEnvironment.Real => new Uri("wss://fstream.binance.com"),
            ProviderEnvironment.Test => new Uri("wss://stream.binancefuture.com"),
            _ => throw new ArgumentException($"Unsupported {env} environment"),
        };
}
