using System;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Internal.Shared;

/// <summary>
/// Base URLs of the Binance spot REST and websocket APIs.
/// </summary>
internal static class Endpoints
{
    /// <summary>Gets the base URL of the spot REST API.</summary>
    public static Uri HttpApi { get; } = new("https://api.binance.com");

    /// <summary>Gets the base URL of the spot websocket API.</summary>
    public static Uri WsApi { get; } = new("wss://stream.binance.com");
}
