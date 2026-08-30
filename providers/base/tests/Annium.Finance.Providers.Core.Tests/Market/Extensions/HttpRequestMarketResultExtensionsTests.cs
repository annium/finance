using System;
using System.Net;
using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Domain.Market.Operations;
using Annium.Finance.Providers.Core.Market.HttpExtensions;
using Annium.Finance.Providers.Tests.Lib;
using Annium.Finance.Providers.Tests.Lib.Infrastructure;
using Annium.Net.Http;
using Annium.Testing;
using OneOf;
using Xunit;

namespace Annium.Finance.Providers.Core.Tests.Market.Extensions;

/// <summary>
/// Pins how <see cref="HttpRequestMarketResultExtensions.AsMarketResultAsync{TData,TError}"/> maps every kind of
/// transport and application outcome - network failure, client-side abort, success, and both parsed and unparsed
/// error bodies - onto a <see cref="MarketResult{T}"/> status, using a real in-process HTTP server.
/// </summary>
public class HttpRequestMarketResultExtensionsTests : ProvidersTestBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HttpRequestMarketResultExtensionsTests"/> class, registering
    /// the HTTP request factory and JSON serializer the tests need to build real requests and parse responses.
    /// </summary>
    /// <param name="outputHelper">The xUnit output helper used to capture test logs.</param>
    public HttpRequestMarketResultExtensionsTests(ITestOutputHelper outputHelper)
        : base(outputHelper)
    {
        this.RegisterHttpRequestFactory();
        this.RegisterJsonSerializer();
    }

    /// <summary>
    /// Verifies that a request sent against a server that never responds (connection torn down before the
    /// response arrives) maps to <see cref="MarketOperationStatus.NetworkError"/> with no data or message.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task NetworkError()
    {
        // arrange
        var server = this.RunHttpServer((_, _) => Task.CompletedTask);
        await server.DisposeAsync();

        // act
        var result = await this.CreateHttpRequest(server)
            .Get("/")
            .AsMarketResultAsync<Response, MarketError>(GetFailure, MapResponse);

        // assert
        result.Status.Is(MarketOperationStatus.NetworkError);
        result.Data.IsDefault();
        result.Message.IsEmpty();
    }

    /// <summary>
    /// Verifies that a request canceled by its own timeout while the server is still processing it maps to
    /// <see cref="MarketOperationStatus.Aborted"/> with no data or message.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task Abort()
    {
        // arrange
        await using var server = this.RunHttpServer((_, _) => Task.Delay(100));

        // act
        var result = await this.CreateHttpRequest(server)
            .Get("/")
            .Timeout(TimeSpan.FromMilliseconds(10))
            .AsMarketResultAsync<Response, MarketError>(GetFailure, MapResponse);

        // assert
        result.Status.Is(MarketOperationStatus.Aborted);
        result.Data.IsDefault();
        result.Message.IsEmpty();
    }

    /// <summary>
    /// Verifies that a 200 OK response with a valid JSON body maps to <see cref="MarketOperationStatus.Ok"/>,
    /// carrying the deserialized data and an empty message.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task SuccessResponse()
    {
        // arrange
        await using var server = this.RunHttpServerWithJsonResponse(HttpStatusCode.OK, new Response("ok"));

        // act
        var result = await this.CreateHttpRequest(server)
            .Get("/")
            .AsMarketResultAsync<Response, MarketError>(GetFailure, MapResponse);

        // assert
        result.Status.Is(MarketOperationStatus.Ok);
        result.Data.IsNotDefault();
        result.Data.Value.Is("ok");
        result.Message.Is(string.Empty);
    }

    /// <summary>
    /// Verifies that a 400 Bad Request response whose body parses as a <see cref="MarketError"/> surfaces that
    /// error's own status (here <see cref="MarketOperationStatus.TooManyRequests"/>) rather than a generic bad
    /// request status, while dropping its message.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ParsedErrorResponse()
    {
        // arrange
        await using var server = this.RunHttpServerWithJsonResponse(
            HttpStatusCode.BadRequest,
            new MarketError(MarketOperationStatus.TooManyRequests, "too many requests")
        );

        // act
        var result = await this.CreateHttpRequest(server)
            .Get("/")
            .AsMarketResultAsync<Response, MarketError>(GetFailure, MapResponse);

        // assert
        result.Status.Is(MarketOperationStatus.BadRequest);
        result.Message.IsEmpty();
    }

    /// <summary>
    /// Verifies that a 400 Bad Request response whose body does not parse as a <see cref="MarketError"/> falls
    /// back to <see cref="MarketOperationStatus.ParseError"/> with a non-empty diagnostic message.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task UnparsedErrorResponse()
    {
        // arrange
        await using var server = this.RunHttpServerWithJsonResponse(HttpStatusCode.BadRequest, "too many requests");

        // act
        var result = await this.CreateHttpRequest(server)
            .Get("/")
            .AsMarketResultAsync<Response, MarketError>(GetFailure, MapResponse);

        // assert
        result.Status.Is(MarketOperationStatus.ParseError);
        result.Message.IsNotEmpty();
    }

    /// <summary>
    /// Builds the <see cref="MarketError"/> passed to <see cref="HttpRequestMarketResultExtensions.AsMarketResultAsync{TData,TError}"/>
    /// for a transport-level failure, mirroring how a real connector would report each failure reason.
    /// </summary>
    /// <param name="reason">The kind of transport-level failure that occurred.</param>
    /// <param name="response">The response associated with the failure.</param>
    /// <param name="exception">The exception that caused the failure, if any.</param>
    /// <returns>The market error describing the failure.</returns>
    private static async Task<MarketError> GetFailure(
        HttpFailureReason reason,
        IHttpResponse response,
        Exception? exception
    )
    {
        var error = reason switch
        {
            HttpFailureReason.Network => new MarketError(
                MarketOperationStatus.NetworkError,
                $"Request not sent ({response.StatusCode} - {response.StatusText})"
            ),
            HttpFailureReason.Abort => new MarketError(
                MarketOperationStatus.Aborted,
                $"Request aborted ({response.StatusCode} - {response.StatusText})"
            ),
            HttpFailureReason.Parse => new MarketError(
                MarketOperationStatus.ParseError,
                $"Response parse failed. Content: {await response.Content.ReadAsStringAsync()}"
            ),
            HttpFailureReason.Exception => new MarketError(
                MarketOperationStatus.ParseError,
                $"Request failed. Error: {exception?.Message}. Content: {await response.Content.ReadAsStringAsync()}"
            ),
            _ => new MarketError(MarketOperationStatus.UnknownError, "Unmapped failure"),
        };

        return error;
    }

    /// <summary>
    /// Maps a response already classified as network error, abort, business error or success into the
    /// <see cref="MarketResult{T}"/> that <see cref="HttpRequestMarketResultExtensions.AsMarketResultAsync{TData,TError}"/>
    /// returns, mirroring how a real connector would interpret the HTTP status code.
    /// </summary>
    /// <param name="response">The classified response to map.</param>
    /// <returns>The resulting market result.</returns>
    private static MarketResult<Response?> MapResponse(IHttpResponse<OneOf<Response, MarketError>> response)
    {
        if (response.IsNetworkError)
            return MarketResult.New<Response?>(MarketOperationStatus.NetworkError, null);

        if (response.IsAbort)
            return MarketResult.New<Response?>(MarketOperationStatus.Aborted, null);

        if (response.Data.IsT1)
        {
            var error = response.Data.AsT1;
            return MarketResult.New<Response?>(error.Status, null, error.Message);
        }

        var data = response.Data.AsT0;

        return response.IsSuccess
            ? MarketResult.Ok<Response?>(data)
            : MarketResult.New<Response?>(MapStatus(response.StatusCode), null);

        static MarketOperationStatus MapStatus(HttpStatusCode code) =>
            code switch
            {
                (HttpStatusCode)418 => MarketOperationStatus.TooManyRequests,
                HttpStatusCode.TooManyRequests => MarketOperationStatus.TooManyRequests,
                HttpStatusCode.BadRequest => MarketOperationStatus.BadRequest,
                _ => MarketOperationStatus.UnknownError,
            };
    }

    /// <summary>Stands in for a provider's success payload; carries a single opaque value the tests round-trip through JSON.</summary>
    /// <param name="Value">The opaque payload value.</param>
    private record Response(string Value);

    /// <summary>Stands in for a provider's business-level error payload, parsed from a non-2xx JSON response body.</summary>
    /// <param name="Status">The market operation status the error maps to.</param>
    /// <param name="Message">The error message.</param>
    private record MarketError(MarketOperationStatus Status, string Message);
}
