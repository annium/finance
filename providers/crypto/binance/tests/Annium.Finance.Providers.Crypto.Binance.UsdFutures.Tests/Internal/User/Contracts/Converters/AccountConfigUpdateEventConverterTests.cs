using System.Text;
using Annium.Finance.Providers.Core;
using Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.User.Contracts.Domain;
using Annium.Finance.Providers.Tests.Lib;
using Annium.Finance.Providers.Tests.Lib.Infrastructure;
using Annium.Testing;
using Xunit;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Tests.Internal.User.Contracts.Converters;

/// <summary>
/// Verifies that <c>AccountConfigUpdateEventConverter</c> reads Binance's <c>ACCOUNT_CONFIG_UPDATE</c>
/// user-data stream event into an <see cref="AccountConfigUpdateEvent"/>, distinguishing the two shapes it
/// carries - a multi-assets-mode toggle under <c>ai</c> versus a per-symbol leverage change under
/// <c>ac</c> - by which field is present, and that an event with a different <c>e</c> type deserializes to
/// null instead of throwing.
/// </summary>
public class AccountConfigUpdateEventConverterTests : ProvidersTestBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AccountConfigUpdateEventConverterTests"/> class.
    /// </summary>
    /// <param name="outputHelper">The xUnit output helper to route trace logging to.</param>
    public AccountConfigUpdateEventConverterTests(ITestOutputHelper outputHelper)
        : base(outputHelper) { }

    /// <summary>
    /// Registers the Binance USD-M futures provider so the converter under test is resolved from its actual registration.
    /// </summary>
    /// <param name="ctx">The fluent context to register providers into.</param>
    protected override void RegisterProvider(ProviderRegistrationContext ctx)
    {
        ctx.WithBinanceUsdFutures();
    }

    /// <summary>
    /// An event carrying the <c>ai</c> field is parsed as a multi-assets-mode change, with the new mode flag.
    /// </summary>
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

    /// <summary>
    /// An event carrying the <c>ac</c> field is parsed as a leverage change, with the affected symbol and
    /// the new leverage.
    /// </summary>
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

    /// <summary>
    /// An event whose <c>e</c> type tag isn't <c>ACCOUNT_CONFIG_UPDATE</c> deserializes to null instead
    /// of throwing.
    /// </summary>
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
