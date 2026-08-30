using System;
using System.Net;
using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Domain.Market.Operations;
using Annium.Finance.Providers.Core;
using Annium.Finance.Providers.Crypto.Binance.Base.Shared.Contracts.Domain;
using Annium.Finance.Providers.Crypto.Binance.Spot.Internal.Market.HttpExtensions;
using Annium.Finance.Providers.Tests.Lib;
using Annium.Finance.Providers.Tests.Lib.Infrastructure;
using Annium.Net.Http;
using Annium.Testing;
using Xunit;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Tests.Internal.Market.HttpExtensions;

/// <summary>
/// Verifies that <c>AsMarketResultAsync</c> maps HTTP outcomes - transport failures, aborted requests,
/// unparsable bodies, Binance error payloads and successful responses - onto the matching
/// <see cref="MarketOperationStatus"/>, against a fully wired Spot provider registration.
/// </summary>
public class HttpRequestMarketResultExtensionsTests : ProvidersTestBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HttpRequestMarketResultExtensionsTests"/> class.
    /// </summary>
    /// <param name="outputHelper">The xUnit output helper to route trace logging to.</param>
    public HttpRequestMarketResultExtensionsTests(ITestOutputHelper outputHelper)
        : base(outputHelper) { }

    /// <summary>
    /// Registers the Binance Spot provider so the extension is exercised through its actual request pipeline.
    /// </summary>
    /// <param name="ctx">The fluent context to register providers into.</param>
    protected override void RegisterProvider(ProviderRegistrationContext ctx)
    {
        ctx.WithBinanceSpot();
    }

    /// <summary>
    /// A request against a server that closed before responding maps to <see cref="MarketOperationStatus.NetworkError"/>.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task NetworkError()
    {
        // arrange
        var server = this.RunHttpServer((_, _) => Task.CompletedTask);
        await server.DisposeAsync();

        // act
        var result = await this.CreateHttpRequest(server, Constants.ServerTimeKey)
            .Get("/")
            .AsMarketResultAsync<ServerTime>();

        // assert
        result.Status.Is(MarketOperationStatus.NetworkError);
        result.Data.IsDefault();
        result.Message.IsNotEmpty();
    }

    /// <summary>
    /// A request that times out before the server responds maps to <see cref="MarketOperationStatus.Aborted"/>.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task Abort()
    {
        // arrange
        await using var server = this.RunHttpServer((_, _) => Task.Delay(100));

        // act
        var result = await this.CreateHttpRequest(server, Constants.ServerTimeKey)
            .Get("/")
            .Timeout(TimeSpan.FromMilliseconds(10))
            .AsMarketResultAsync<ServerTime>();

        // assert
        result.Status.Is(MarketOperationStatus.Aborted);
        result.Data.IsDefault();
        result.Message.IsNotEmpty();
    }

    /// <summary>
    /// An error response whose body doesn't parse as the expected Binance error payload maps to
    /// <see cref="MarketOperationStatus.ParseError"/>, whether the body is not JSON at all or just an empty object.
    /// </summary>
    /// <param name="body">The unparsable response body.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Theory]
    [InlineData("not json")]
    [InlineData("{}")]
    public async Task UnparsedErrorResponse(string body)
    {
        // arrange
        await using var server = this.RunHttpServerWithJsonResponse(HttpStatusCode.BadRequest, body);

        // act
        var result = await this.CreateHttpRequest(server, Constants.ServerTimeKey)
            .Get("/")
            .AsMarketResultAsync<ServerTime>();

        // assert
        result.Status.Is(MarketOperationStatus.ParseError);
        result.Message.IsNotEmpty();
    }

    /// <summary>
    /// A Binance error payload is mapped by its numeric code: a negative code to
    /// <see cref="MarketOperationStatus.BadRequest"/>, any other recognized code to
    /// <see cref="MarketOperationStatus.UnknownError"/>, with the payload message passed through unchanged.
    /// </summary>
    /// <param name="code">The Binance error code carried in the payload.</param>
    /// <param name="status">The status the code is expected to map to.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Theory]
    [InlineData(-1, MarketOperationStatus.BadRequest)]
    [InlineData(10, MarketOperationStatus.UnknownError)]
    public async Task OperationResultResponse(long code, MarketOperationStatus status)
    {
        // arrange
        await using var server = this.RunHttpServerWithJsonResponse(
            HttpStatusCode.BadRequest,
            new { code, msg = "error" }
        );

        // act
        var result = await this.CreateHttpRequest(server, Constants.ServerTimeKey)
            .Get("/")
            .AsMarketResultAsync<ServerTime>();

        // assert
        result.Status.Is(status);
        result.Message.Is("error");
    }

    /// <summary>
    /// A 200 response with a well-formed body is deserialized into its data, with an empty message and
    /// <see cref="MarketOperationStatus.Ok"/>.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task SuccessResponse()
    {
        // arrange
        await using var server = this.RunHttpServerWithJsonResponse(HttpStatusCode.OK, new { serverTime = 20 });

        // act
        var result = await this.CreateHttpRequest(server, Constants.ServerTimeKey)
            .Get("/")
            .AsMarketResultAsync<ServerTime>();

        // assert
        result.Status.Is(MarketOperationStatus.Ok);
        result.Data.IsNotDefault();
        result.Data.Value.Is(20);
        result.Message.IsEmpty();
    }

    /// <summary>
    /// A non-2xx HTTP status is mapped to the matching status even when the body still parses as valid
    /// data, since Binance can return an error status code alongside a well-formed payload; 418 is
    /// Binance's IP-ban status and is treated the same as the standard 429.
    /// </summary>
    /// <param name="code">The HTTP status code returned by the server.</param>
    /// <param name="status">The status the code is expected to map to.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Theory]
    [InlineData(HttpStatusCode.BadRequest, MarketOperationStatus.BadRequest)]
    [InlineData((HttpStatusCode)418, MarketOperationStatus.TooManyRequests)]
    [InlineData(HttpStatusCode.TooManyRequests, MarketOperationStatus.TooManyRequests)]
    [InlineData(HttpStatusCode.InternalServerError, MarketOperationStatus.UnknownError)]
    public async Task FailedSuccessResponse(HttpStatusCode code, MarketOperationStatus status)
    {
        // arrange
        await using var server = this.RunHttpServerWithJsonResponse(code, new { serverTime = 20 });

        // act
        var result = await this.CreateHttpRequest(server, Constants.ServerTimeKey)
            .Get("/")
            .AsMarketResultAsync<ServerTime>();

        // assert
        result.Status.Is(status);
        result.Data.IsNotDefault();
        result.Data.Value.Is(20);
        result.Message.IsEmpty();
    }
}
