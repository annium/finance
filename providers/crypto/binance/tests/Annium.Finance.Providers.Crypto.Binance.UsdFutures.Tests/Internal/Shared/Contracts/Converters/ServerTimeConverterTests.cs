using System.Text;
using Annium.Finance.Providers.Core;
using Annium.Finance.Providers.Crypto.Binance.Base.Shared.Contracts.Domain;
using Annium.Finance.Providers.Tests.Lib;
using Annium.Finance.Providers.Tests.Lib.Infrastructure;
using Annium.Testing;
using Xunit;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Tests.Internal.Shared.Contracts.Converters;

public class ServerTimeConverterTests : ProvidersTestBase
{
    public ServerTimeConverterTests(ITestOutputHelper outputHelper)
        : base(outputHelper) { }

    protected override void RegisterProvider(ProviderRegistrationContext ctx)
    {
        ctx.WithBinanceUsdFutures();
    }

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
