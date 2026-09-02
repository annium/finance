using System;
using Annium.Core.Mapper;
using Annium.Finance.Providers.Abstractions.Domain.Market;
using Annium.Finance.Providers.Abstractions.Domain.User;
using Annium.Finance.Providers.Core;
using Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Market;
using Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Shared;
using Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.User;
using Annium.Finance.Providers.Tests.Lib;
using Annium.Testing;
using Xunit;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Tests.Internal.Shared;

/// <summary>
/// Pins the websocket URLs this provider actually connects to, composed the way the connection code
/// composes them.
/// </summary>
/// <remarks>
/// Binance split the futures websocket into routed endpoints - <c>/public</c>, <c>/market</c>,
/// <c>/private</c> - and decommissioned the unrouted legacy URLs on 2026-04-23. Nothing here noticed: no
/// test in this repository opens a user data stream, so the user connector went on connecting to a dead
/// URL, staying open, and receiving nothing. A drift check found it; no test could have.
///
/// These assert the composed URL rather than the configured parts, because the composition is where the
/// mistake hides. <c>new Uri(base, path)</c> replaces the base's path when the path begins with a slash,
/// so a route held in the base would be silently dropped - producing exactly the legacy URL this change
/// exists to stop using, from configuration that reads as correct.
/// </remarks>
public class EndpointsTests : ProvidersTestBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EndpointsTests"/> class.
    /// </summary>
    /// <param name="outputHelper">The xUnit output helper used to capture test logs.</param>
    public EndpointsTests(ITestOutputHelper outputHelper)
        : base(outputHelper) { }

    /// <summary>
    /// Registers the USD-M futures provider, so the configuration profiles under test are the registered ones.
    /// </summary>
    /// <param name="ctx">The fluent context to register providers into.</param>
    protected override void RegisterProvider(ProviderRegistrationContext ctx)
    {
        ctx.WithBinanceUsdFutures();
    }

    /// <summary>
    /// The market stream connects to the public route. <c>@bookTicker</c>, the only stream this provider
    /// subscribes to, is the category Binance routes there; a connection without the route receives it and
    /// nothing else, which is why a market stream on the legacy URL looks healthy.
    /// </summary>
    [Fact]
    public void MarketStream_ConnectsToThePublicRoute()
    {
        // arrange
        var config = Get<IMapper>().Map<MarketConfig>(new MarketSettings { Provider = Constants.Provider });

        // act - exactly as WebSocketService composes it
        var uri = new Uri(config.WsApi, config.WsUriPath);

        // assert
        uri.ToString().Is("wss://fstream.binance.com/public/stream");
    }

    /// <summary>
    /// The user data stream connects to the private route. This is the one that cost us: on the legacy URL
    /// it delivers no order and no account update at all, while the connector reports itself connected.
    /// </summary>
    [Fact]
    public void UserStream_ConnectsToThePrivateRoute()
    {
        // arrange
        var config = Get<IMapper>()
            .Map<UserConfig>(
                new UserSettings
                {
                    Provider = Constants.Provider,
                    Key = "some_key",
                    Secret = "some_secret",
                }
            );

        // act - exactly as UserStream composes it, listen key and all
        var uri = new Uri(config.WsApi, config.ListenKeyUriPath + "SOME_LISTEN_KEY");

        // assert
        uri.ToString().Is("wss://fstream.binance.com/private/ws/SOME_LISTEN_KEY");
    }

    /// <summary>
    /// The server time path is the one the exchange documents for this venue. It was wrong on the spot side
    /// for as long as nobody ran it: the manifest recorded the version oddity as a divergence and never
    /// checked it against the documented endpoint list, so a curiosity stood in for a verified fact until a
    /// live run failed on it. Pinned on both venues now, since the two genuinely differ.
    /// </summary>
    [Fact]
    public void ServerTime_IsAskedForAtTheDocumentedPath()
    {
        // assert
        new Uri(Endpoints.HttpApi, Endpoints.ServerTimeUriPath)
            .ToString()
            .Is("https://fapi.binance.com/fapi/v1/time");
    }

    /// <summary>
    /// The REST base is unrouted, and stays that way - the split applies to the websocket only.
    /// </summary>
    [Fact]
    public void RestApi_IsUnrouted()
    {
        // arrange
        var config = Get<IMapper>().Map<MarketConfig>(new MarketSettings { Provider = Constants.Provider });

        // assert
        config.HttpApi.ToString().Is("https://fapi.binance.com/");
    }
}
