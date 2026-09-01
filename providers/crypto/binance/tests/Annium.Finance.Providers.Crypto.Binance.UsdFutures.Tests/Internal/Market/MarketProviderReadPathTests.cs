using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using Annium.Core.DependencyInjection;
using Annium.Finance.Providers.Abstractions.Domain.Market.Operations;
using Annium.Finance.Providers.Core;
using Annium.Finance.Providers.Core.Shared.RateLimits;
using Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Market;
using Annium.Finance.Providers.Tests.Lib;
using Annium.Finance.Providers.Tests.Lib.Infrastructure;
using Annium.Logging;
using Annium.Net.Http;
using Annium.Net.Servers.Web;
using Annium.Testing;
using NodaTime;
using Xunit;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Tests.Internal.Market;

/// <summary>
/// Drives the USD-M futures market provider's read paths against a local HTTP server, so what it does with
/// an exchange response can be asserted without the exchange.
/// </summary>
/// <remarks>
/// Every provider test in this repository was gated behind the live exchange, which meant an ordinary run
/// proved nothing about any of them. These cover the parts a verification census found nothing watching at
/// all: that the rate-limit ceiling is taken from the response rather than left at its configured default,
/// that an instrument the exchange is not trading is dropped, that one missing a filter we depend on is
/// dropped too, and that candle paging asks for what it has not yet received.
/// </remarks>
public class MarketProviderReadPathTests : ProvidersTestBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MarketProviderReadPathTests"/> class.
    /// </summary>
    /// <param name="outputHelper">The xUnit output helper used to capture test logs.</param>
    public MarketProviderReadPathTests(ITestOutputHelper outputHelper)
        : base(outputHelper) { }

    /// <summary>
    /// Registers the USD-M futures provider, so the serializers and request factories under test are the
    /// registered ones rather than stand-ins.
    /// </summary>
    /// <param name="ctx">The fluent context to register providers into.</param>
    protected override void RegisterProvider(ProviderRegistrationContext ctx)
    {
        ctx.WithBinanceUsdFutures();
    }

    /// <summary>
    /// The weight ceiling comes from the response, not from the value compiled in at registration. Binance
    /// changes these, and a limiter left at a stale ceiling either throttles a connection that had room or,
    /// worse, lets one past a limit that has since been lowered.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task LoadContext_TakesTheWeightCeilingFromTheResponse()
    {
        // arrange
        var limiter = new RecordingRateLimiter();
        await using var server = ServeJson(ExchangeInfo(weightLimit: 4321));
        var provider = CreateProvider(server, limiter);

        // act
        var result = await provider.LoadContextAsync();

        // assert
        result.Status.Is(MarketOperationStatus.Ok);
        limiter.Limits.IsEqual(new[] { 4321 });
    }

    /// <summary>
    /// A margin asset the instruments do not already describe becomes a resource, with a precision this
    /// provider guesses: two digits when the code contains "USD", eight otherwise.
    /// </summary>
    /// <remarks>
    /// The guess is ours, not Binance's — the exchange-info response carries no precision for these assets.
    /// It is pinned here because an undocumented assumption that nothing tests can change under us twice
    /// over: once when the exchange alters what it sends, and once when someone edits the heuristic.
    /// </remarks>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task LoadContext_GuessesAPrecisionForAssetsTheInstrumentsDoNotDescribe()
    {
        // arrange - BTC and USDT are described by the symbol; ETH is only ever a margin asset
        await using var server = ServeJson(ExchangeInfo());
        var provider = CreateProvider(server);

        // act
        var context = (await provider.LoadContextAsync()).Data.NotNull();

        // assert
        var eth = context.Resources.Single(x => x.Code == "ETH");
        eth.Precision.Is((byte)8, "an asset without USD in its code is guessed at eight digits");
        var usdt = context.Resources.Single(x => x.Code == "USDT");
        usdt.Precision.Is((byte)8, "USDT is described by the symbol, so its own precision wins over the guess");
    }

    /// <summary>
    /// An instrument the exchange is not currently trading is dropped. Binance has ten contract statuses and
    /// only one of them is <c>TRADING</c>; the rest — halted, settling, closed — describe a symbol that
    /// exists and cannot be traded, and this provider offers no way to say that, so it offers nothing.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task LoadContext_DropsAnInstrumentThatIsNotTrading()
    {
        // arrange
        await using var server = ServeJson(ExchangeInfo(status: "TRADING_HALT"));
        var provider = CreateProvider(server);

        // act
        var context = (await provider.LoadContextAsync()).Data.NotNull();

        // assert
        context.Instruments.IsEmpty("a halted symbol must not be offered as tradable");
    }

    /// <summary>
    /// An instrument missing a filter this provider depends on is dropped whole, rather than surfacing with a
    /// bound of zero. Worth pinning because the two are easy to confuse: a bound the exchange does not
    /// enforce does arrive as zero, but an absent filter removes the symbol entirely.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task LoadContext_DropsAnInstrumentMissingAFilterItNeeds()
    {
        // arrange
        await using var server = ServeJson(ExchangeInfo(withNotionalFilter: false));
        var provider = CreateProvider(server);

        // act
        var context = (await provider.LoadContextAsync()).Data.NotNull();

        // assert
        context.Instruments.IsEmpty("a symbol without the notional filter must not be offered at all");
    }

    /// <summary>
    /// Candle loading pages: each request asks from the minute after the last candle received, and the
    /// enumeration ends when the range is covered.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task LoadCandles_AsksFromWhereTheLastPageEnded()
    {
        // arrange - one candle per response, so every page boundary is a request we can observe
        var start = Instant.FromUnixTimeMilliseconds(1_700_000_000_000);
        var minute = Duration.FromMinutes(1);
        var requested = new List<long>();

        await using var server = this.RunHttpServer(
            async (request, response) =>
            {
                var from = long.Parse(request.QueryString["startTime"]!, CultureInfo.InvariantCulture);
                requested.Add(from);
                await WriteJsonAsync(response, $"[[{from},\"1\",\"2\",\"0.5\",\"1.5\",\"10\"]]");
            }
        );
        var provider = CreateProvider(server);

        // act
        var batches = new List<int>();
        await foreach (
            var batch in provider.LoadCandlesAsync(
                "BTCUSDT",
                start,
                start + minute * 3,
                TestContext.Current.CancellationToken
            )
        )
            batches.Add(batch.Data.NotNull().Count);

        // assert - three minutes asked for, one candle at a time, each request starting where the last ended
        requested.IsEqual(
            new[]
            {
                start.ToUnixTimeMilliseconds(),
                (start + minute).ToUnixTimeMilliseconds(),
                (start + minute * 2).ToUnixTimeMilliseconds(),
            }
        );
        batches.Count.Is(3);
    }

    /// <summary>
    /// Builds a market provider pointed at the given local server.
    /// </summary>
    /// <param name="server">The local server standing in for the exchange.</param>
    /// <param name="limiter">The rate limiter to hand the provider; a real one when not given.</param>
    /// <returns>The provider under test.</returns>
    private MarketProvider CreateProvider(IServer server, IRateLimiter? limiter = null)
    {
        var sp = Get<IServiceProvider>();
        var config = new MarketConfig
        {
            Provider = Constants.Provider,
            HttpApi = server.HttpUri(),
            WsApi = new Uri("wss://unused"),
            WsUriPath = "/unused",
        };

        return new MarketProvider(
            config,
            sp.ResolveHttpRequestFactory(Constants.ExchangeInfoKey),
            sp.ResolveHttpRequestFactory(Constants.CandleKey),
            limiter ?? sp.Resolve<IRateLimiter>(),
            Logger
        );
    }

    /// <summary>Starts a server answering every request with the given JSON body.</summary>
    /// <param name="json">The body to answer with.</param>
    /// <returns>The running server.</returns>
    private IServer ServeJson(string json) =>
        this.RunHttpServer(async (_, response) => await WriteJsonAsync(response, json));

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
    /// Builds an exchange-info payload carrying one symbol, varying only what a test is about.
    /// </summary>
    /// <param name="weightLimit">The request-weight limit the response reports.</param>
    /// <param name="status">The symbol's contract status.</param>
    /// <param name="withNotionalFilter">Whether the symbol carries the notional filter.</param>
    /// <returns>The payload, as JSON.</returns>
    private static string ExchangeInfo(
        int weightLimit = 2400,
        string status = "TRADING",
        bool withNotionalFilter = true
    )
    {
        var notional = withNotionalFilter
            ? @",{ ""notional"": ""5.0"", ""filterType"": ""MIN_NOTIONAL"" }"
            : string.Empty;

        return $@"{{
            ""rateLimits"": [ {{ ""rateLimitType"": ""REQUEST_WEIGHT"", ""interval"": ""MINUTE"", ""intervalNum"": 1, ""limit"": {weightLimit} }} ],
            ""assets"": [ {{ ""asset"": ""USDT"", ""marginAvailable"": true }}, {{ ""asset"": ""ETH"", ""marginAvailable"": true }} ],
            ""symbols"": [ {{
                ""symbol"": ""BTCUSDT"",
                ""contractType"": ""PERPETUAL"",
                ""status"": ""{status}"",
                ""baseAsset"": ""BTC"",
                ""quoteAsset"": ""USDT"",
                ""baseAssetPrecision"": 8,
                ""quotePrecision"": 8,
                ""filters"": [
                    {{ ""minPrice"": ""556.80"", ""maxPrice"": ""4529764"", ""filterType"": ""PRICE_FILTER"", ""tickSize"": ""0.10"" }},
                    {{ ""stepSize"": ""0.001"", ""filterType"": ""LOT_SIZE"", ""maxQty"": ""1000"", ""minQty"": ""0.001"" }},
                    {{ ""stepSize"": ""0.001"", ""filterType"": ""MARKET_LOT_SIZE"", ""maxQty"": ""120"", ""minQty"": ""0.001"" }},
                    {{ ""limit"": 200, ""filterType"": ""MAX_NUM_ORDERS"" }}{notional}
                ]
            }} ]
        }}";
    }

    /// <summary>A rate limiter that records what limits it was given, and permits everything.</summary>
    private sealed class RecordingRateLimiter : IRateLimiter
    {
        /// <summary>Gets every limit this limiter has been told to use, in order.</summary>
        public List<int> Limits { get; } = [];

        /// <summary>Always allows a request.</summary>
        /// <returns>Always <see langword="true"/>.</returns>
        public bool CanExecute() => true;

        /// <summary>Records the limit.</summary>
        /// <param name="limit">The limit reported by the exchange.</param>
        public void UpdateLimit(int limit) => Limits.Add(limit);

        /// <summary>Ignores the reported weight; this limiter never throttles.</summary>
        /// <param name="weight">Ignored.</param>
        public void UsedWeight(int weight) { }

        /// <summary>Nothing to release.</summary>
        public void Dispose() { }
    }
}
