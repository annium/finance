using System;
using Annium.Finance.Providers.Crypto.Binance.Spot.Internal.Shared;
using Annium.Testing;
using Xunit;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Tests.Internal.Shared;

/// <summary>
/// Pins the spot endpoints, composed the way the code composes them.
/// </summary>
/// <remarks>
/// The server time path was <c>/api/v1/time</c> here while every other spot call is <c>v3</c>. The contract
/// manifest recorded that as a divergence — a curiosity worth noting — and never compared it against the
/// documented endpoint list, so it stood as a fact for as long as nothing ran it. The first live read run
/// failed on it. A test costs less than a live run and answers sooner.
/// </remarks>
public class EndpointsTests
{
    /// <summary>
    /// Server time is asked for at the documented spot path, which is <c>v3</c> like the rest of the venue —
    /// and unlike futures, where <c>v1</c> is correct.
    /// </summary>
    [Fact]
    public void ServerTime_IsAskedForAtTheDocumentedPath()
    {
        // assert
        new Uri(Endpoints.HttpApi, Endpoints.ServerTimeUriPath)
            .ToString()
            .Is("https://api.binance.com/api/v3/time");
    }

    /// <summary>
    /// The market websocket base is unrouted: the routed split Binance introduced applies to futures only.
    /// </summary>
    [Fact]
    public void MarketWebsocket_IsUnrouted()
    {
        // assert
        Endpoints.WsApi.ToString().Is("wss://stream.binance.com/");
    }
}
