using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using Annium.Core.DependencyInjection;
using Annium.Finance.Providers.Abstractions.Domain.Market.Operations;
using Annium.Finance.Providers.Core;
using Annium.Finance.Providers.Core.Shared.RateLimits;
using Annium.Finance.Providers.Crypto.Binance.Spot.Internal.Market;
using Annium.Finance.Providers.Tests.Lib;
using Annium.Finance.Providers.Tests.Lib.Infrastructure;
using Annium.Net.Http;
using Annium.Net.Servers.Web;
using Annium.Testing;
using Xunit;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Tests.Internal.Market;

/// <summary>
/// Drives the spot market provider's read paths against a local HTTP server.
/// </summary>
/// <remarks>
/// Spot and futures look alike here and are not: the notional filter has a different type name and a
/// different field, the tradability rules are different, and spot alone requires the <c>SPOT</c> permission.
/// A test on the futures side says nothing about any of it, which is why these exist separately rather than
/// being shared.
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
    /// Registers the Binance spot provider, so the serializers and request factories are the registered ones.
    /// </summary>
    /// <param name="ctx">The fluent context to register providers into.</param>
    protected override void RegisterProvider(ProviderRegistrationContext ctx)
    {
        ctx.WithBinanceSpot();
    }

    /// <summary>
    /// The weight ceiling comes from the response rather than the value compiled in at registration.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task LoadContext_TakesTheWeightCeilingFromTheResponse()
    {
        // arrange
        var limiter = new RecordingRateLimiter();
        await using var server = ServeJson(ExchangeInfo(weightLimit: 1234));
        var provider = CreateProvider(server, limiter);

        // act
        var result = await provider.LoadContextAsync();

        // assert
        result.Status.Is(MarketOperationStatus.Ok);
        limiter.Limits.IsEqual(new[] { 1234 });
    }

    /// <summary>
    /// A symbol the exchange is not trading is dropped. Spot has five symbol statuses and only
    /// <c>TRADING</c> is one this provider can offer.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task LoadContext_DropsASymbolThatIsNotTrading()
    {
        // arrange
        await using var server = ServeJson(ExchangeInfo(status: "HALT"));
        var provider = CreateProvider(server);

        // act
        var context = (await provider.LoadContextAsync()).Data.NotNull();

        // assert
        context.Instruments.IsEmpty("a halted symbol must not be offered as tradable");
    }

    /// <summary>
    /// A symbol without spot trading permission is dropped, however tradable it looks otherwise. This has no
    /// futures counterpart at all — there, the equivalent question is the contract type.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task LoadContext_DropsASymbolWithoutSpotPermission()
    {
        // arrange
        await using var server = ServeJson(ExchangeInfo(spotAllowed: false));
        var provider = CreateProvider(server);

        // act
        var context = (await provider.LoadContextAsync()).Data.NotNull();

        // assert
        context.Instruments.IsEmpty("a symbol that cannot be spot-traded must not be offered");
    }

    /// <summary>
    /// The notional bounds come from spot's <c>NOTIONAL</c> filter, which carries both a minimum and a
    /// maximum — where futures has <c>MIN_NOTIONAL</c>, one field, and a synthesized maximum.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task LoadContext_ReadsBothNotionalBounds()
    {
        // arrange
        await using var server = ServeJson(ExchangeInfo());
        var provider = CreateProvider(server);

        // act
        var context = (await provider.LoadContextAsync()).Data.NotNull();

        // assert
        var instrument = context.Instruments.Single();
        instrument.MinSum.Is(5m);
        instrument.MaxSum.Is(9_000_000m, "spot reports a maximum notional and it must not be discarded");
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
    /// Builds a spot exchange-info payload carrying one symbol, varying only what a test is about.
    /// </summary>
    /// <param name="weightLimit">The request-weight limit the response reports.</param>
    /// <param name="status">The symbol's status.</param>
    /// <param name="spotAllowed">Whether the symbol permits spot trading.</param>
    /// <returns>The payload, as JSON.</returns>
    private static string ExchangeInfo(int weightLimit = 6000, string status = "TRADING", bool spotAllowed = true)
    {
        var permissions = spotAllowed ? @"[ ""SPOT"" ]" : @"[ ""MARGIN"" ]";

        return $@"{{
            ""rateLimits"": [ {{ ""rateLimitType"": ""REQUEST_WEIGHT"", ""interval"": ""MINUTE"", ""intervalNum"": 1, ""limit"": {weightLimit} }} ],
            ""symbols"": [ {{
                ""symbol"": ""BTCUSDT"",
                ""status"": ""{status}"",
                ""baseAsset"": ""BTC"",
                ""baseAssetPrecision"": 8,
                ""quoteAsset"": ""USDT"",
                ""quoteAssetPrecision"": 8,
                ""isSpotTradingAllowed"": {(spotAllowed ? "true" : "false")},
                ""permissions"": {permissions},
                ""filters"": [
                    {{ ""minPrice"": ""0.01"", ""maxPrice"": ""1000000"", ""filterType"": ""PRICE_FILTER"", ""tickSize"": ""0.01"" }},
                    {{ ""stepSize"": ""0.00001"", ""filterType"": ""LOT_SIZE"", ""maxQty"": ""9000"", ""minQty"": ""0.00001"" }},
                    {{ ""stepSize"": ""0.00001"", ""filterType"": ""MARKET_LOT_SIZE"", ""maxQty"": ""100"", ""minQty"": ""0.00001"" }},
                    {{ ""limit"": 200, ""filterType"": ""MAX_NUM_ORDERS"" }},
                    {{ ""minNotional"": ""5.0"", ""maxNotional"": ""9000000"", ""filterType"": ""NOTIONAL"" }}
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
