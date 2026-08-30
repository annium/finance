using System;
using System.Net;
using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Domain.User.Operations;
using Annium.Finance.Providers.Core.User.HttpExtensions;
using Annium.Finance.Providers.Tests.Lib;
using Annium.Finance.Providers.Tests.Lib.Infrastructure;
using Annium.Net.Http;
using Annium.Testing;
using OneOf;
using Xunit;

namespace Annium.Finance.Providers.Core.Tests.User.Extensions;

/// <summary>
/// Pins how <see cref="HttpRequestUserResultExtensions.AsUserResultAsync{TData,TError}"/> maps every kind of
/// transport and application outcome - network failure, client-side abort, success, and both parsed and unparsed
/// error bodies - onto a <see cref="UserResult{T}"/> status, using a real in-process HTTP server.
/// </summary>
public class HttpRequestUserResultExtensionsTests : ProvidersTestBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HttpRequestUserResultExtensionsTests"/> class, registering
    /// the HTTP request factory and JSON serializer the tests need to build real requests and parse responses.
    /// </summary>
    /// <param name="outputHelper">The xUnit output helper used to capture test logs.</param>
    public HttpRequestUserResultExtensionsTests(ITestOutputHelper outputHelper)
        : base(outputHelper)
    {
        this.RegisterHttpRequestFactory();
        this.RegisterJsonSerializer();
    }

    /// <summary>
    /// Verifies that a request sent against a server that never responds (connection torn down before the
    /// response arrives) maps to <see cref="UserOperationStatus.NetworkError"/> with no data or message.
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
            .AsUserResultAsync<Response, UserError>(GetFailure, MapResponse);

        // assert
        result.Status.Is(UserOperationStatus.NetworkError);
        result.Data.IsDefault();
        result.Message.IsEmpty();
    }

    /// <summary>
    /// Verifies that a request canceled by its own timeout while the server is still processing it maps to
    /// <see cref="UserOperationStatus.Aborted"/> with no data or message.
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
            .AsUserResultAsync<Response, UserError>(GetFailure, MapResponse);

        // assert
        result.Status.Is(UserOperationStatus.Aborted);
        result.Data.IsDefault();
        result.Message.IsEmpty();
    }

    /// <summary>
    /// Verifies that a 200 OK response with a valid JSON body maps to <see cref="UserOperationStatus.Ok"/>,
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
            .AsUserResultAsync<Response, UserError>(GetFailure, MapResponse);

        // assert
        result.Status.Is(UserOperationStatus.Ok);
        result.Data.IsNotDefault();
        result.Data.Value.Is("ok");
        result.Message.Is(string.Empty);
    }

    /// <summary>
    /// Verifies that a 400 Bad Request response whose body parses as a <see cref="UserError"/> surfaces that
    /// error's own status (here <see cref="UserOperationStatus.TooManyRequests"/>) rather than a generic bad
    /// request status, while dropping its message.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ParsedErrorResponse()
    {
        // arrange
        await using var server = this.RunHttpServerWithJsonResponse(
            HttpStatusCode.BadRequest,
            new UserError(UserOperationStatus.TooManyRequests, "too many requests")
        );

        // act
        var result = await this.CreateHttpRequest(server)
            .Get("/")
            .AsUserResultAsync<Response, UserError>(GetFailure, MapResponse);

        // assert
        result.Status.Is(UserOperationStatus.BadRequest);
        result.Message.IsEmpty();
    }

    /// <summary>
    /// Verifies that a 400 Bad Request response whose body does not parse as a <see cref="UserError"/> falls
    /// back to <see cref="UserOperationStatus.ParseError"/> with a non-empty diagnostic message.
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
            .AsUserResultAsync<Response, UserError>(GetFailure, MapResponse);

        // assert
        result.Status.Is(UserOperationStatus.ParseError);
        result.Message.IsNotEmpty();
    }

    /// <summary>
    /// Builds the <see cref="UserError"/> passed to <see cref="HttpRequestUserResultExtensions.AsUserResultAsync{TData,TError}"/>
    /// for a transport-level failure, mirroring how a real connector would report each failure reason.
    /// </summary>
    /// <param name="reason">The kind of transport-level failure that occurred.</param>
    /// <param name="response">The response associated with the failure.</param>
    /// <param name="exception">The exception that caused the failure, if any.</param>
    /// <returns>The user error describing the failure.</returns>
    private static async Task<UserError> GetFailure(
        HttpFailureReason reason,
        IHttpResponse response,
        Exception? exception
    )
    {
        var error = reason switch
        {
            HttpFailureReason.Network => new UserError(
                UserOperationStatus.NetworkError,
                $"Request not sent ({response.StatusCode} - {response.StatusText})"
            ),
            HttpFailureReason.Abort => new UserError(
                UserOperationStatus.Aborted,
                $"Request aborted ({response.StatusCode} - {response.StatusText})"
            ),
            HttpFailureReason.Parse => new UserError(
                UserOperationStatus.ParseError,
                $"Response parse failed. Content: {await response.Content.ReadAsStringAsync()}"
            ),
            HttpFailureReason.Exception => new UserError(
                UserOperationStatus.ParseError,
                $"Request failed. Error: {exception?.Message}. Content: {await response.Content.ReadAsStringAsync()}"
            ),
            _ => new UserError(UserOperationStatus.UnknownError, "Unmapped failure"),
        };

        return error;
    }

    /// <summary>
    /// Maps a response already classified as network error, abort, business error or success into the
    /// <see cref="UserResult{T}"/> that <see cref="HttpRequestUserResultExtensions.AsUserResultAsync{TData,TError}"/>
    /// returns, mirroring how a real connector would interpret the HTTP status code.
    /// </summary>
    /// <param name="response">The classified response to map.</param>
    /// <returns>The resulting user result.</returns>
    private static UserResult<Response?> MapResponse(IHttpResponse<OneOf<Response, UserError>> response)
    {
        if (response.IsNetworkError)
            return UserResult.New<Response?>(UserOperationStatus.NetworkError, null);

        if (response.IsAbort)
            return UserResult.New<Response?>(UserOperationStatus.Aborted, null);

        if (response.Data.IsT1)
        {
            var error = response.Data.AsT1;
            return UserResult.New<Response?>(error.Status, null, error.Message);
        }

        var data = response.Data.AsT0;

        return response.IsSuccess
            ? UserResult.Ok<Response?>(data)
            : UserResult.New<Response?>(MapStatus(response.StatusCode), null);

        static UserOperationStatus MapStatus(HttpStatusCode code) =>
            code switch
            {
                (HttpStatusCode)418 => UserOperationStatus.TooManyRequests,
                HttpStatusCode.TooManyRequests => UserOperationStatus.TooManyRequests,
                HttpStatusCode.BadRequest => UserOperationStatus.BadRequest,
                _ => UserOperationStatus.UnknownError,
            };
    }

    /// <summary>Stands in for a provider's success payload; carries a single opaque value the tests round-trip through JSON.</summary>
    /// <param name="Value">The opaque payload value.</param>
    private record Response(string Value);

    /// <summary>Stands in for a provider's business-level error payload, parsed from a non-2xx JSON response body.</summary>
    /// <param name="Status">The user operation status the error maps to.</param>
    /// <param name="Message">The error message.</param>
    private record UserError(UserOperationStatus Status, string Message);
}
