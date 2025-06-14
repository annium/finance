using System.Text;
using Annium.Finance.Providers.Crypto.Binance.Base.Contracts.Shared.Domain;
using Annium.Finance.Providers.Tests.Shared.Connectors;
using Annium.Finance.Providers.Tests.Shared.Extensions;
using Annium.Testing;
using Xunit;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Tests.Internal.Contracts.Shared.Converters;

public class ServerTimeConverterTests : ConnectorTestBase
{
    public ServerTimeConverterTests(ITestOutputHelper outputHelper)
        : base(ctx => ctx.WithBinanceUsdFutures(), outputHelper) { }

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
        var deserialized = serializer.Deserialize<ServerTime?>(Encoding.UTF8.GetBytes(raw)).NotNull();

        // assert - deserialization
        deserialized.Value.Is(123);
    }

    [Fact]
    public void InvalidDataReturnsEmpty()
    {
        // arrange
        var raw =
            @"{
            ""time"":123
        }";

        // act - deserialize
        var serializer = this.GetJsonSerializer(Constants.ServerTimeKey);
        var deserialized = serializer.Deserialize<ServerTime?>(Encoding.UTF8.GetBytes(raw));

        // assert - deserialization
        deserialized.IsDefault();
    }
}
