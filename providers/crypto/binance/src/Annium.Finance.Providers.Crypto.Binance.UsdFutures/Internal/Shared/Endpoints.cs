using System;
using Annium.Finance.Providers.Abstractions.Domain.Shared;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Shared;

/// <summary>
/// Base URLs of the Binance USD-M futures REST and websocket APIs, per provider environment.
/// </summary>
internal static class Endpoints
{
    /// <summary>
    /// Resolves the base URL of the USD-M futures REST API for the given environment.
    /// </summary>
    /// <param name="env">The provider environment (production or testnet).</param>
    /// <returns>The base URL of the REST API.</returns>
    public static Uri GetHttpApi(ProviderEnvironment env) =>
        env switch
        {
            ProviderEnvironment.Real => new Uri("https://fapi.binance.com"),
            ProviderEnvironment.Test => new Uri("https://testnet.binancefuture.com"),
            _ => throw new ArgumentException($"Unsupported {env} environment"),
        };

    /// <summary>
    /// Resolves the base URL of the USD-M futures websocket API for the given environment.
    /// </summary>
    /// <param name="env">The provider environment (production or testnet).</param>
    /// <returns>The base URL of the websocket API.</returns>
    public static Uri GetWsApi(ProviderEnvironment env) =>
        env switch
        {
            ProviderEnvironment.Real => new Uri("wss://fstream.binance.com"),
            ProviderEnvironment.Test => new Uri("wss://stream.binancefuture.com"),
            _ => throw new ArgumentException($"Unsupported {env} environment"),
        };
}
