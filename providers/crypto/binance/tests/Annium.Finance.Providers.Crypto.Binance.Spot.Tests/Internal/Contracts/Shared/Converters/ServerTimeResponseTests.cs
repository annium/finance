using System.Text;
using Annium.Finance.Providers.Crypto.Binance.Base.Contracts.Shared.Domain;
using Annium.Finance.Providers.Tests.Shared.Connectors;
using Annium.Finance.Providers.Tests.Shared.Extensions;
using Annium.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Tests.Internal.Contracts.Shared.Converters;

public class ServerTimeResponseTests : ConnectorTestBase
{
    public ServerTimeResponseTests(ITestOutputHelper outputHelper)
        : base(ctx => ctx.WithBinanceSpot(), outputHelper) { }

    [Fact]
    public void Works()
    {
        // arrange
        var raw =
            @"{
            ""serverTime"":123
        }";

        // act - deserialize
        var serializer = this.GetJsonSerializer(Constants.ServerTimeKey);
        var deserialized = serializer.Deserialize<ServerTime>(Encoding.UTF8.GetBytes(raw)).NotNull();

        // assert - deserialization
        deserialized.Value.Is(123);
    }
}
