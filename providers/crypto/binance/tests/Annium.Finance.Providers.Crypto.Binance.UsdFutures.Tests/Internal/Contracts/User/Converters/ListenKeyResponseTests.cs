using System.Text;
using Annium.Finance.Providers.Core;
using Annium.Finance.Providers.Crypto.Binance.Base.User.Contracts.Domain;
using Annium.Finance.Providers.Tests.Lib;
using Annium.Finance.Providers.Tests.Lib.Infrastructure;
using Annium.Testing;
using Xunit;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Tests.Internal.Contracts.User.Converters;

public class ListenKeyResponseTests : ProvidersTestBase
{
    public ListenKeyResponseTests(ITestOutputHelper outputHelper)
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
            ""listenKey"":""pqia91ma19a5s61cv6a81va65sdf19v8a65a1a5s61cv6a81va65sdf19v8a65a1""
        }";

        // act - deserialize
        var serializer = this.GetJsonSerializer(Constants.ListenKeyKey);
        var deserialized = serializer.Deserialize<ListenKey>(Encoding.UTF8.GetBytes(raw)).NotNull();

        // assert - deserialization
        deserialized.Value.Is("pqia91ma19a5s61cv6a81va65sdf19v8a65a1a5s61cv6a81va65sdf19v8a65a1");
    }
}
