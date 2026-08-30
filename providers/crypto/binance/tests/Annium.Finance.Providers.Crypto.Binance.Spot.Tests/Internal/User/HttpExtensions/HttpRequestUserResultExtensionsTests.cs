using System;
using System.Net;
using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Domain.User.Operations;
using Annium.Finance.Providers.Core;
using Annium.Finance.Providers.Crypto.Binance.Base.Shared.Contracts.Domain;
using Annium.Finance.Providers.Crypto.Binance.Spot.Internal.User.HttpExtensions;
using Annium.Finance.Providers.Tests.Lib;
using Annium.Finance.Providers.Tests.Lib.Infrastructure;
using Annium.Net.Http;
using Annium.Testing;
using Xunit;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Tests.Internal.User.HttpExtensions;

/// <summary>
/// Verifies that <c>AsUserResultAsync</c> maps HTTP outcomes - transport failures, aborted requests,
/// unparsable bodies, Binance error payloads and successful responses - onto the matching
/// <see cref="UserOperationStatus"/>, against a fully wired Spot provider registration.
/// </summary>
public class HttpRequestUserResultExtensionsTests : ProvidersTestBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HttpRequestUserResultExtensionsTests"/> class.
    /// </summary>
    /// <param name="outputHelper">The xUnit output helper to route trace logging to.</param>
    public HttpRequestUserResultExtensionsTests(ITestOutputHelper outputHelper)
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
    /// A request to a port nothing is listening on - refused outright - maps to <see cref="UserOperationStatus.NetworkError"/>.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task NetworkError()
    {
        // arrange
        // act
        var result = await this.CreateHttpRequestToClosedPort(Constants.ServerTimeKey)
            .Get("/")
            .AsUserResultAsync<ServerTime>();

        // assert
        result.Status.Is(UserOperationStatus.NetworkError);
        result.Data.IsDefault();
        result.Message.IsNotEmpty();
    }

    /// <summary>
    /// A request that times out before the server responds maps to <see cref="UserOperationStatus.Aborted"/>.
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
            .AsUserResultAsync<ServerTime>();

        // assert
        result.Status.Is(UserOperationStatus.Aborted);
        result.Data.IsDefault();
        result.Message.IsNotEmpty();
    }

    /// <summary>
    /// An error response whose body doesn't parse as the expected Binance error payload maps to
    /// <see cref="UserOperationStatus.ParseError"/>, whether the body is not JSON at all or just an empty object.
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
            .AsUserResultAsync<ServerTime>();

        // assert
        result.Status.Is(UserOperationStatus.ParseError);
        result.Message.IsNotEmpty();
    }

    /// <summary>
    /// A Binance error payload is mapped by its numeric code: a negative code to
    /// <see cref="UserOperationStatus.BadRequest"/>, the two account-balance codes (-2018, -2019) to
    /// <see cref="UserOperationStatus.InsufficientBalance"/>, and any other recognized code to
    /// <see cref="UserOperationStatus.UnknownError"/>, with the payload message passed through unchanged.
    /// </summary>
    /// <param name="code">The Binance error code carried in the payload.</param>
    /// <param name="status">The status the code is expected to map to.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Theory]
    [InlineData(-1, UserOperationStatus.BadRequest)]
    [InlineData(-2018, UserOperationStatus.InsufficientBalance)]
    [InlineData(-2019, UserOperationStatus.InsufficientBalance)]
    [InlineData(10, UserOperationStatus.UnknownError)]
    public async Task OperationResultResponse(long code, UserOperationStatus status)
    {
        // arrange
        await using var server = this.RunHttpServerWithJsonResponse(
            HttpStatusCode.BadRequest,
            new { code, msg = "error" }
        );

        // act
        var result = await this.CreateHttpRequest(server, Constants.ServerTimeKey)
            .Get("/")
            .AsUserResultAsync<ServerTime>();

        // assert
        result.Status.Is(status);
        result.Message.Is("error");
    }

    /// <summary>
    /// A 200 response with a well-formed body is deserialized into its data, with an empty message and
    /// <see cref="UserOperationStatus.Ok"/>.
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
            .AsUserResultAsync<ServerTime>();

        // assert
        result.Status.Is(UserOperationStatus.Ok);
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
    [InlineData(HttpStatusCode.BadRequest, UserOperationStatus.BadRequest)]
    [InlineData((HttpStatusCode)418, UserOperationStatus.TooManyRequests)]
    [InlineData(HttpStatusCode.TooManyRequests, UserOperationStatus.TooManyRequests)]
    [InlineData(HttpStatusCode.InternalServerError, UserOperationStatus.UnknownError)]
    public async Task FailedSuccessResponse(HttpStatusCode code, UserOperationStatus status)
    {
        // arrange
        await using var server = this.RunHttpServerWithJsonResponse(code, new { serverTime = 20 });

        // act
        var result = await this.CreateHttpRequest(server, Constants.ServerTimeKey)
            .Get("/")
            .AsUserResultAsync<ServerTime>();

        // assert
        result.Status.Is(status);
        result.Data.IsNotDefault();
        result.Data.Value.Is(20);
        result.Message.IsEmpty();
    }
}
