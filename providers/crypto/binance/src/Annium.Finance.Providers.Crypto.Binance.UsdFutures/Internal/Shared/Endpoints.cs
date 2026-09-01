using System;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Shared;

/// <summary>
/// Base URLs of the Binance USD-M futures REST and websocket APIs.
/// </summary>
/// <remarks>
/// The websocket base carries no route of its own. Binance split its futures websocket into three routed
/// endpoints - <c>/public</c> for high-frequency public market data, <c>/market</c> for the regular market
/// feeds, <c>/private</c> for user data - and decommissioned the unrouted legacy URLs on 2026-04-23. A
/// connection made without a routed path receives only what <c>/public</c> carries, so a user stream on the
/// legacy base connects, stays open, and delivers nothing at all.
///
/// The route belongs to the path rather than to this base, and deliberately so: the paths are combined here
/// with <c>new Uri(base, path)</c>, where a path beginning with a slash replaces the base's path entirely. A
/// route held in the base would be silently dropped at composition - which looks exactly like the bug this
/// class exists to fix.
///
/// Which route a stream belongs to is Binance's classification, not ours: <c>@bookTicker</c>, the only market
/// stream this provider subscribes to, is listed under Public. Subscribing to a stream from another category
/// means another connection, not another topic on this one.
/// </remarks>
internal static class Endpoints
{
    /// <summary>Gets the base URL of the USD-M futures REST API.</summary>
    public static Uri HttpApi { get; } = new("https://fapi.binance.com");

    /// <summary>Gets the base URL of the USD-M futures websocket API, without a route.</summary>
    public static Uri WsApi { get; } = new("wss://fstream.binance.com");

    /// <summary>The routed path of the combined public market stream, appended to <see cref="WsApi"/>.</summary>
    public const string MarketWsUriPath = "/public/stream";

    /// <summary>The routed path prefix of the user data stream, to which the listen key is appended.</summary>
    public const string UserWsUriPath = "/private/ws/";
}
