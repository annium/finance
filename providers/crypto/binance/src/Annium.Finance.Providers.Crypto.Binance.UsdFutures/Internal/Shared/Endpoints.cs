using System;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Shared;

/// <summary>
/// Base URLs of the Binance USD-M futures REST and websocket APIs.
/// </summary>
internal static class Endpoints
{
    /// <summary>Gets the base URL of the USD-M futures REST API.</summary>
    public static Uri HttpApi { get; } = new("https://fapi.binance.com");

    /// <summary>Gets the base URL of the USD-M futures websocket API.</summary>
    public static Uri WsApi { get; } = new("wss://fstream.binance.com");
}
