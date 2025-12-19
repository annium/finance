using System;
using System.Net;
using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Domain.User.Operations;
using Annium.Finance.Providers.Shared.Connectors.Extensions;
using Annium.Finance.Providers.Tests.Lib;
using Annium.Finance.Providers.Tests.Lib.TestBaseExtensions;
using Annium.Net.Http;
using Annium.Testing;
using OneOf;
using Xunit;

namespace Annium.Finance.Providers.Shared.Tests.Connectors.Extensions;

public class HttpRequestUserResultExtensionsTests : ProvidersTestBase
{
    public HttpRequestUserResultExtensionsTests(ITestOutputHelper outputHelper)
        : base(outputHelper)
    {
        this.RegisterHttpRequestFactory();
        this.RegisterJsonSerializer();
    }

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

    private record Response(string Value);

    private record UserError(UserOperationStatus Status, string Message);
}
