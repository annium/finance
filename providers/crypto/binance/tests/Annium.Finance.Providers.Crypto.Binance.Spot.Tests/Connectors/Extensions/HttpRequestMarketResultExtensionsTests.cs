using System;
using System.Net;
using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Domain.Operations;
using Annium.Finance.Providers.Crypto.Binance.Base.Contracts.Shared.Domain;
using Annium.Finance.Providers.Crypto.Binance.Spot.Internal.Connectors.Extensions;
using Annium.Finance.Providers.Shared;
using Annium.Finance.Providers.Tests.Lib;
using Annium.Finance.Providers.Tests.Lib.TestBaseExtensions;
using Annium.Net.Http;
using Annium.Testing;
using Xunit;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Tests.Connectors.Extensions;

public class HttpRequestMarketResultExtensionsTests : ProvidersTestBase
{
    public HttpRequestMarketResultExtensionsTests(ITestOutputHelper outputHelper)
        : base(outputHelper) { }

    protected override void RegisterProvider(ProviderRegistrationContext ctx)
    {
        ctx.WithBinanceSpot();
    }

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
