using System;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Internal.Shared;

/// <summary>
/// Base URLs and fixed paths of the Binance spot REST and websocket APIs.
/// </summary>
internal static class Endpoints
{
    /// <summary>Gets the base URL of the spot REST API.</summary>
    public static Uri HttpApi { get; } = new("https://api.binance.com");

    /// <summary>Gets the base URL of the spot websocket API.</summary>
    public static Uri WsApi { get; } = new("wss://stream.binance.com");

    /// <summary>
    /// The server time path, appended to <see cref="HttpApi"/>.
    /// </summary>
    /// <remarks>
    /// <c>v3</c>, like the rest of the spot surface. It was <c>v1</c> here for as long as nobody ran the
    /// thing: the contract manifest recorded the oddity as a divergence and never checked it against the
    /// documented endpoint list, so a curiosity stood in for a verified fact until a live run failed on it.
    /// </remarks>
    public const string ServerTimeUriPath = "/api/v3/time";
}
