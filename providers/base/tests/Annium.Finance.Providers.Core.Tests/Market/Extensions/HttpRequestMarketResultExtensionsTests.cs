using System;
using System.Net;
using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Domain.Market.Operations;
using Annium.Finance.Providers.Core.Market.Extensions;
using Annium.Finance.Providers.Tests.Lib;
using Annium.Finance.Providers.Tests.Lib.TestBaseExtensions;
using Annium.Net.Http;
using Annium.Testing;
using OneOf;
using Xunit;

namespace Annium.Finance.Providers.Core.Tests.Market.Extensions;

public class HttpRequestMarketResultExtensionsTests : ProvidersTestBase
{
    public HttpRequestMarketResultExtensionsTests(ITestOutputHelper outputHelper)
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
            .AsMarketResultAsync<Response, MarketError>(GetFailure, MapResponse);

        // assert
        result.Status.Is(MarketOperationStatus.NetworkError);
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
            .AsMarketResultAsync<Response, MarketError>(GetFailure, MapResponse);

        // assert
        result.Status.Is(MarketOperationStatus.Aborted);
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
            .AsMarketResultAsync<Response, MarketError>(GetFailure, MapResponse);

        // assert
        result.Status.Is(MarketOperationStatus.Ok);
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

    private record Response(string Value);

    private record MarketError(MarketOperationStatus Status, string Message);
}
