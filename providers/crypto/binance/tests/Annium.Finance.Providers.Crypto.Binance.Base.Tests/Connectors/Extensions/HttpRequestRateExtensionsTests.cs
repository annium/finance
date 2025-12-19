using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Annium.Finance.Providers.Core.Shared.RateLimits;
using Annium.Finance.Providers.Crypto.Binance.Base.Connectors.Extensions;
using Annium.Finance.Providers.Tests.Lib;
using Annium.Finance.Providers.Tests.Lib.Infrastructure;
using Annium.Net.Http;
using Annium.Net.Servers.Web;
using Annium.Testing;
using Xunit;

namespace Annium.Finance.Providers.Crypto.Binance.Base.Tests.Connectors.Extensions;

public class HttpRequestRateExtensionsTests : ProvidersTestBase
{
    public HttpRequestRateExtensionsTests(ITestOutputHelper outputHelper)
        : base(outputHelper)
    {
        Register(container => container.AddHttpRequestFactory(true));
    }

    [Fact]
    public async Task CantExecute_ReturnsTooManyRequestsAndSkipsServer()
    {
        // arrange
        var limiter = new FakeRateLimiter(false);
        var serverHits = 0;

        await using var server = this.RunHttpServer(
            (_, response) =>
            {
                serverHits++;
                response.Ok();
                return Task.CompletedTask;
            }
        );

        // act
        var response = await SendAsync(server, limiter);

        // assert
        response.StatusCode.Is(HttpStatusCode.TooManyRequests);
        serverHits.Is(0);
        limiter.UsedWeights.IsEmpty();
    }

    [Theory]
    [InlineData("x-mbx-used-weight-1m")]
    [InlineData("X-MBX-Used-Weight-1M")]
    public async Task ValidHeader_SetsUsedWeight(string header)
    {
        // arrange
        var limiter = new FakeRateLimiter(true);

        await using var server = this.RunHttpServer(
            (_, response) =>
            {
                response.Headers.Add(header, "123");
                response.Ok();
                return Task.CompletedTask;
            }
        );

        // act
        var httpResponse = await SendAsync(server, limiter);

        // assert
        httpResponse.StatusCode.Is(HttpStatusCode.OK);
        limiter.UsedWeights.Has(1);
        limiter.UsedWeights.At(0).Is(123);
    }

    [Fact]
    public async Task MissingHeader_DoesNotUpdateWeight()
    {
        // arrange
        var limiter = new FakeRateLimiter(true);

        await using var server = this.RunHttpServer(
            (_, response) =>
            {
                response.Ok();
                return Task.CompletedTask;
            }
        );

        // act
        await SendAsync(server, limiter);

        // assert
        limiter.UsedWeights.IsEmpty();
    }

    [Fact]
    public async Task InvalidHeaderValue_DoesNotUpdateWeight()
    {
        // arrange
        var limiter = new FakeRateLimiter(true);

        await using var server = this.RunHttpServer(
            (_, response) =>
            {
                response.Headers.Add("x-mbx-used-weight-1m", "abc");
                response.Ok();
                return Task.CompletedTask;
            }
        );

        // act
        await SendAsync(server, limiter);

        // assert
        limiter.UsedWeights.IsEmpty();
    }

    private Task<IHttpResponse> SendAsync(IServer server, FakeRateLimiter limiter)
    {
        var request = this.CreateHttpRequest(server).Get("rate-limit").WithRateDelay1M(limiter);

        return request.RunAsync();
    }

    private sealed class FakeRateLimiter : IRateLimiter
    {
        public IReadOnlyList<int> UsedWeights => _usedWeights;
        private readonly List<int> _usedWeights = [];
        private readonly bool _canExecute;

        public FakeRateLimiter(bool canExecute)
        {
            _canExecute = canExecute;
        }

        public void Dispose() { }

        public bool CanExecute() => _canExecute;

        public void UpdateLimit(int limit) { }

        public void UsedWeight(int weight) => _usedWeights.Add(weight);
    }
}
