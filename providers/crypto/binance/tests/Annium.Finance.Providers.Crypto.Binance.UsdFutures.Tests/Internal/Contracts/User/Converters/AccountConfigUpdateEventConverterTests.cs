using System.Text;
using Annium.Finance.Providers.Core;
using Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.User.Contracts.Domain;
using Annium.Finance.Providers.Tests.Lib;
using Annium.Finance.Providers.Tests.Lib.Infrastructure;
using Annium.Testing;
using Xunit;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Tests.Internal.Contracts.User.Converters;

public class AccountConfigUpdateEventConverterTests : ProvidersTestBase
{
    public AccountConfigUpdateEventConverterTests(ITestOutputHelper outputHelper)
        : base(outputHelper) { }

    protected override void RegisterProvider(ProviderRegistrationContext ctx)
    {
        ctx.WithBinanceUsdFutures();
    }

    [Fact]
    public void Works_MultiAssetsModeChange()
    {
        // arrange
        var raw =
            @"{
            ""e"":""ACCOUNT_CONFIG_UPDATE"",
            ""E"":1611646737479,
            ""T"":1611646737476,
            ""ai"":{
                ""j"":true
            }
        }";

        // act - deserialize
        var serializer = this.GetJsonSerializer(Constants.AccountConfigurationUpdateKey);
        var deserialized = serializer.Deserialize<AccountConfigUpdateEvent>(Encoding.UTF8.GetBytes(raw)).NotNull();

        // assert - deserialization
        deserialized.Date.Is(1611646737476);
        deserialized.Type.Is(AccountConfigUpdateEventType.MultiAssetsModeChange);
        deserialized.MultiAssetsMode.IsTrue();
    }

    [Fact]
    public void Works_LeverageChange()
    {
        // arrange
        var raw =
            @"{
            ""e"":""ACCOUNT_CONFIG_UPDATE"",
            ""E"":1611646737479,
            ""T"":1611646737476,
            ""ac"":{
                ""s"":""BTCUSDT"",
                ""l"":25
            }
        }";

        // act - deserialize
        var serializer = this.GetJsonSerializer(Constants.AccountConfigurationUpdateKey);
        var deserialized = serializer.Deserialize<AccountConfigUpdateEvent>(Encoding.UTF8.GetBytes(raw)).NotNull();

        // assert - deserialization
        deserialized.Date.Is(1611646737476);
        deserialized.Type.Is(AccountConfigUpdateEventType.LeverageChange);
        deserialized.Symbol.Is("BTCUSDT");
        deserialized.Leverage.Is(25);
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
        var serializer = this.GetJsonSerializer(Constants.AccountConfigurationUpdateKey);
        var deserialized = serializer.Deserialize<AccountConfigUpdateEvent>(Encoding.UTF8.GetBytes(raw));

        // assert - deserialization
        deserialized.IsDefault();
    }
}
