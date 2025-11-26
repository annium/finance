using System.Linq;
using System.Text;
using Annium.Finance.Providers.Crypto.Binance.Spot.Internal.Contracts.User.Domain;
using Annium.Finance.Providers.Shared;
using Annium.Finance.Providers.Tests.Lib;
using Annium.Finance.Providers.Tests.Lib.TestBaseExtensions;
using Annium.Testing;
using Xunit;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Tests.Internal.Contracts.User.Converters;

public class AccountUpdateEventConverterTests : ProvidersTestBase
{
    public AccountUpdateEventConverterTests(ITestOutputHelper outputHelper)
        : base(outputHelper) { }

    protected override void RegisterProvider(ProviderRegistrationContext ctx)
    {
        ctx.WithBinanceSpot();
    }

    [Fact]
    public void Works()
    {
        // arrange
        var raw =
            @"{
            ""e"": ""outboundAccountPosition"",
            ""E"": 1564034571105,
            ""u"": 1564034571073,
            ""B"": [
                {
                    ""a"": ""ETH"",
                    ""f"": ""10.500000"",
                    ""l"": ""1.700000""
                }
            ]
        }";

        // act - deserialize
        var serializer = this.GetJsonSerializer(Constants.AccountUpdateKey);
        var deserialized = serializer.Deserialize<AccountUpdateEvent>(Encoding.UTF8.GetBytes(raw)).NotNull();

        // assert - deserialization
        deserialized.Date.Is(1564034571073);
        deserialized.Balances.Has(1);
        var eth = deserialized.Balances.ElementAt(0);
        eth.IsEqual(new AccountUpdateEventBalance("ETH", 10.5m, 1.7m));
    }

    [Fact]
    public void SkipsInvalidData()
    {
        // arrange
        var raw =
            @"{
            ""e"": ""invalid""
        }";

        // act - deserialize
        var serializer = this.GetJsonSerializer(Constants.AccountUpdateKey);
        var deserialized = serializer.Deserialize<AccountUpdateEvent>(Encoding.UTF8.GetBytes(raw));

        // assert - deserialization
        deserialized.IsDefault();
    }
}
