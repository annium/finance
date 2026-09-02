using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using Annium;
using Annium.Core.DependencyInjection;
using Annium.Finance.Providers.Abstractions.Domain.User.Operations;
using Annium.Finance.Providers.Core;
using Annium.Finance.Providers.Core.Shared.Loaders;
using Annium.Finance.Providers.Core.Shared.RateLimits;
using Annium.Finance.Providers.Crypto.Binance.Base.User.Services;
using Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.User;
using Annium.Finance.Providers.Tests.Lib;
using Annium.Finance.Providers.Tests.Lib.Infrastructure;
using Annium.Net.Http;
using Annium.Net.Servers.Web;
using Annium.Testing;
using NodaTime;
using Xunit;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Tests.Internal.User;

/// <summary>
/// Drives the USD-M futures user provider's history paging against a local HTTP server.
/// </summary>
/// <remarks>
/// The gated suite covers this path in name only: it asks for a single day of history, which never reaches
/// the seven-day chunk boundary it claims to protect, so even with the exchange switch on it can confirm
/// nothing beyond the call not erroring. A verification census classified it <c>vacuous</c> for exactly that
/// reason. Here the window is driven from outside, so the boundaries are observable.
/// </remarks>
public class UserProviderReadPathTests : ProvidersTestBase
{
    /// <summary>Seven days, in milliseconds — the window the provider chunks history by.</summary>
    private const long Window = 7L * 24 * 60 * 60 * 1000;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserProviderReadPathTests"/> class.
    /// </summary>
    /// <param name="outputHelper">The xUnit output helper used to capture test logs.</param>
    public UserProviderReadPathTests(ITestOutputHelper outputHelper)
        : base(outputHelper) { }

    /// <summary>
    /// Registers the USD-M futures provider, so the serializers and request factories are the registered ones.
    /// </summary>
    /// <param name="ctx">The fluent context to register providers into.</param>
    protected override void RegisterProvider(ProviderRegistrationContext ctx)
    {
        ctx.WithBinanceUsdFutures();
    }

    /// <summary>
    /// History longer than one window is asked for a window at a time, each request continuing where the last
    /// ended, and the final one stopping at the range's end rather than overrunning it.
    /// </summary>
    /// <remarks>
    /// Binance rejects a history query spanning more than seven days, so a provider that stopped chunking
    /// would not return partial data — it would fail outright, on accounts with enough history and nowhere
    /// else. The kind of defect that reaches production because the fixture that should have caught it asked
    /// for a day.
    /// </remarks>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task LoadOrders_ChunksHistoryByTheSevenDayWindow()
    {
        // arrange - twenty days back, so the range needs three windows
        var windows = new List<(long Start, long End)>();
        await using var server = this.RunHttpServer(
            async (request, response) =>
            {
                var start = request.QueryString["startTime"];
                var end = request.QueryString["endTime"];
                if (start is not null && end is not null)
                    windows.Add(
                        (long.Parse(start, CultureInfo.InvariantCulture), long.Parse(end, CultureInfo.InvariantCulture))
                    );

                await WriteJsonAsync(response, "[]");
            }
        );
        var provider = CreateProvider(server);
        var since = SystemClock.Instance.GetCurrentInstant() - Duration.FromDays(20);

        // act
        var result = await provider.LoadOrdersAsync("BTCUSDT", since.ToUnixTimeMilliseconds());

        // assert
        result.Status.Is(UserOperationStatus.Ok);
        windows.Count.Is(3, "twenty days of history needs three seven-day windows");

        // every window but the last is exactly one window wide
        foreach (var (start, end) in windows.Take(windows.Count - 1))
            (end - start).Is(Window, "a window that is not the last must span the full seven days");

        // and each continues where the previous ended, with the last stopping short rather than overrunning
        for (var i = 1; i < windows.Count; i++)
            windows[i].Start.Is(windows[i - 1].End, $"window {i} must continue from where window {i - 1} ended");
        (windows[^1].End - windows[^1].Start < Window).IsTrue("the final window must stop at the range's end");
    }

    /// <summary>
    /// Asking for the latest orders rather than a history takes a different path: one request, no window
    /// bounds at all.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task LoadOrders_WithoutASince_AsksOnceAndUnbounded()
    {
        // arrange
        var calls = 0;
        var boundedCalls = 0;
        await using var server = this.RunHttpServer(
            async (request, response) =>
            {
                calls++;
                if (request.QueryString["startTime"] is not null)
                    boundedCalls++;

                await WriteJsonAsync(response, "[]");
            }
        );
        var provider = CreateProvider(server);

        // act
        await provider.LoadOrdersAsync("BTCUSDT", null);

        // assert
        calls.Is(1);
        boundedCalls.Is(0, "the latest page is not a time range and must carry no window bounds");
    }

    /// <summary>
    /// Trade history is chunked by the same window as order history, and by the same code — but a test on
    /// one says nothing about the other, so both are driven.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task LoadTrades_ChunksHistoryByTheSevenDayWindow()
    {
        // arrange
        var windows = new List<(long Start, long End)>();
        await using var server = this.RunHttpServer(
            async (request, response) =>
            {
                var start = request.QueryString["startTime"];
                var end = request.QueryString["endTime"];
                if (start is not null && end is not null)
                    windows.Add(
                        (long.Parse(start, CultureInfo.InvariantCulture), long.Parse(end, CultureInfo.InvariantCulture))
                    );

                await WriteJsonAsync(response, "[]");
            }
        );
        var provider = CreateProvider(server);
        var since = SystemClock.Instance.GetCurrentInstant() - Duration.FromDays(20);

        // act
        var result = await provider.LoadTradesAsync("BTCUSDT", since.ToUnixTimeMilliseconds());

        // assert
        result.Status.Is(UserOperationStatus.Ok);
        windows.Count.Is(3);
        foreach (var (start, end) in windows.Take(windows.Count - 1))
            (end - start).Is(Window);
        for (var i = 1; i < windows.Count; i++)
            windows[i].Start.Is(windows[i - 1].End);
    }

    /// <summary>
    /// An asset's locked balance is the initial and maintenance margin added together — arithmetic of ours,
    /// not a field the exchange sends, so no converter test sees it.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task LoadContext_LocksInitialAndMaintenanceMarginTogether()
    {
        // arrange - a wallet with 100 available, 3 of initial margin and 2 of maintenance
        await using var server = this.RunHttpServer(
            async (_, response) =>
                await WriteJsonAsync(
                    response,
                    @"{
                        ""assets"": [ {
                            ""asset"": ""USDT"",
                            ""marginBalance"": ""105"",
                            ""maxWithdrawAmount"": ""100"",
                            ""initialMargin"": ""3"",
                            ""maintMargin"": ""2"",
                            ""updateTime"": 0
                        } ],
                        ""positions"": []
                    }"
                )
        );
        var provider = CreateProvider(server);

        // act
        var context = (await provider.LoadContextAsync()).Data.NotNull();

        // assert
        var usdt = context.Assets.Single(x => x.Resource == "USDT");
        usdt.Free.Is(100m, "the free balance is what can be withdrawn");
        usdt.Locked.Is(5m, "the locked balance is initial and maintenance margin together, not either alone");
    }

    /// <summary>
    /// Builds a user provider pointed at the given local server.
    /// </summary>
    /// <param name="server">The local server standing in for the exchange.</param>
    /// <returns>The provider under test.</returns>
    private UserProvider CreateProvider(IServer server)
    {
        var sp = Get<IServiceProvider>();
        var reload = new CompositeLoaderConfig(1, 2, 5, 0, 0);
        var config = new UserConfig
        {
            Provider = Constants.Provider,
            Key = "some_key",
            Secret = "some_secret",
            HttpApi = server.HttpUri(),
            WsApi = new Uri("wss://unused"),
            ListenKeyUriPath = "/unused/",
            ListenKey = new ListenKeyConfiguration(1000, 1000),
            ReloadContext = reload,
            ReloadOrders = reload,
            ReloadTrades = reload,
        };

        return new UserProvider(
            config,
            sp.Resolve<ITimeProvider>(),
            new StubSignatureService(),
            sp.ResolveHttpRequestFactory(Constants.GetAccountKey),
            sp.ResolveHttpRequestFactory(Constants.GetOrderKey),
            sp.ResolveHttpRequestFactory(Constants.GetTradeKey),
            sp.Resolve<IRateLimiter>(),
            Logger
        );
    }

    /// <summary>Writes a JSON body and a 200 to the response.</summary>
    /// <param name="response">The response to write to.</param>
    /// <param name="json">The body to write.</param>
    /// <returns>A task representing the write.</returns>
    private static async Task WriteJsonAsync(HttpListenerResponse response, string json)
    {
        var payload = Encoding.UTF8.GetBytes(json);
        response.StatusCode(HttpStatusCode.OK);
        response.ContentType = MediaTypeNames.Application.Json;
        response.ContentLength64 = payload.Length;
        await response.OutputStream.WriteAsync(payload);
    }

    /// <summary>
    /// Signs nothing, so no request in this fixture needs a server time source.
    /// </summary>
    /// <remarks>
    /// Resolving the real one would start it polling the exchange's public time endpoint from its
    /// constructor, which is how an offline test reaches the network without anyone intending it.
    /// </remarks>
    private sealed class StubSignatureService : ISignatureService
    {
        /// <summary>Gets a fixed timestamp, standing in for synced server time.</summary>
        public long ServerTime => 1_700_000_000_000;

        /// <summary>Returns a fixed API key.</summary>
        /// <returns>The key.</returns>
        public string GetKey() => "some_key";

        /// <summary>Returns a fixed signature for any data.</summary>
        /// <param name="data">Ignored.</param>
        /// <returns>The signature.</returns>
        public string GetSignature(string data) => "signature";
    }
}
