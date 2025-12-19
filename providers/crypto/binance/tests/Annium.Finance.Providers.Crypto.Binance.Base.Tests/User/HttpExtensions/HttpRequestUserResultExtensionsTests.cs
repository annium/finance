using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Domain.User.Operations;
using Annium.Finance.Providers.Core;
using Annium.Finance.Providers.Crypto.Binance.Base.Internal.User.HttpExtensions;
using Annium.Finance.Providers.Crypto.Binance.Base.Shared.Contracts.Converters;
using Annium.Finance.Providers.Crypto.Binance.Base.Shared.Contracts.Domain;
using Annium.Finance.Providers.Tests.Lib;
using Annium.Finance.Providers.Tests.Lib.Infrastructure;
using Annium.Net.Http;
using Annium.Serialization.Json;
using Annium.Testing;
using Xunit;

namespace Annium.Finance.Providers.Crypto.Binance.Base.Tests.User.HttpExtensions;

public class HttpRequestUserResultExtensionsTests : ProvidersTestBase
{
    public HttpRequestUserResultExtensionsTests(ITestOutputHelper outputHelper)
        : base(outputHelper) { }

    protected override void RegisterProvider(ProviderRegistrationContext ctx)
    {
        ctx.AddHttpRequestFactoryWithJsonSerializer(
            string.Empty,
            new JsonSerializerOptions()
                .ResetConverters()
                .AddConverter<ServerTimeConverter>()
                .AddConverter<OperationResultConverter>()
        );
    }

    [Fact]
    public async Task NetworkError()
    {
        // arrange
        var server = this.RunHttpServer((_, _) => Task.CompletedTask);
        await server.DisposeAsync();

        // act
        var result = await this.CreateHttpRequest(server).Get("/").AsUserResultAsync<ServerTime>();

        // assert
        result.Status.Is(UserOperationStatus.NetworkError);
        result.Data.IsDefault();
        result.Message.IsNotEmpty();
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
            .AsUserResultAsync<ServerTime>();

        // assert
        result.Status.Is(UserOperationStatus.Aborted);
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
        var result = await this.CreateHttpRequest(server).Get("/").AsUserResultAsync<ServerTime>();

        // assert
        result.Status.Is(UserOperationStatus.ParseError);
        result.Message.IsNotEmpty();
    }

    [Theory]
    [InlineData(-1, UserOperationStatus.BadRequest)]
    [InlineData(10, UserOperationStatus.UnknownError)]
    public async Task OperationResultResponse(long code, UserOperationStatus status)
    {
        // arrange
        await using var server = this.RunHttpServerWithJsonResponse(
            HttpStatusCode.BadRequest,
            new { code, msg = "error" }
        );

        // act
        var result = await this.CreateHttpRequest(server).Get("/").AsUserResultAsync<ServerTime>();

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
        var result = await this.CreateHttpRequest(server).Get("/").AsUserResultAsync<ServerTime>();

        // assert
        result.Status.Is(UserOperationStatus.Ok);
        result.Data.IsNotDefault();
        result.Data.Value.Is(20);
        result.Message.IsEmpty();
    }

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
        var result = await this.CreateHttpRequest(server).Get("/").AsUserResultAsync<ServerTime>();

        // assert
        result.Status.Is(status);
        result.Data.IsNotDefault();
        result.Data.Value.Is(20);
        result.Message.IsEmpty();
    }
}
