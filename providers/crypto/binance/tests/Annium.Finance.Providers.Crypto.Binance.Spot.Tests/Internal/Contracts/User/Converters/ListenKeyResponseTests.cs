using System.Text;
using Annium.Finance.Providers.Crypto.Binance.Base.Contracts.User.Domain;
using Annium.Finance.Providers.Tests.Shared.Connectors;
using Annium.Finance.Providers.Tests.Shared.Extensions;
using Annium.Testing;
using Xunit;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Tests.Internal.Contracts.User.Converters;

public class ListenKeyResponseTests : ConnectorTestBase
{
    public ListenKeyResponseTests(ITestOutputHelper outputHelper)
        : base(ctx => ctx.WithBinanceSpot(), outputHelper) { }

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
