using System;
using Annium.Finance.Providers.Abstractions.Domain.Shared;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Internal.Shared;

/// <summary>Resolves the Binance spot HTTP and WebSocket base URLs for the real and testnet environments.</summary>
internal static class Endpoints
{
    /// <summary>Gets the Binance spot REST API base URL for the given environment.</summary>
    /// <param name="env">The provider environment to resolve the URL for.</param>
    /// <returns>The HTTP API base URL.</returns>
    public static Uri GetHttpApi(ProviderEnvironment env) =>
        env switch
        {
            ProviderEnvironment.Real => new Uri("https://api.binance.com"),
            ProviderEnvironment.Test => new Uri("https://testnet.binance.vision"),
            _ => throw new ArgumentException($"Unsupported {env} environment"),
        };

    /// <summary>Gets the Binance spot WebSocket API base URL for the given environment.</summary>
    /// <param name="env">The provider environment to resolve the URL for.</param>
    /// <returns>The WebSocket API base URL.</returns>
    public static Uri GetWsApi(ProviderEnvironment env) =>
        env switch
        {
            ProviderEnvironment.Real => new Uri("wss://stream.binance.com"),
            ProviderEnvironment.Test => new Uri("wss://testnet.binance.vision"),
            _ => throw new ArgumentException($"Unsupported {env} environment"),
        };
}
