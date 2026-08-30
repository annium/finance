using System.Linq;
using System.Text;
using Annium.Finance.Providers.Core;
using Annium.Finance.Providers.Crypto.Binance.Spot.Internal.User.Contracts.Domain;
using Annium.Finance.Providers.Tests.Lib;
using Annium.Finance.Providers.Tests.Lib.Infrastructure;
using Annium.Testing;
using Xunit;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Tests.Internal.User.Contracts.Converters;

/// <summary>
/// Verifies that <c>AccountUpdateEventConverter</c> reads Binance's <c>outboundAccountPosition</c> user-data
/// stream event - an event type tag plus a list of per-asset free/locked balances - into an
/// <see cref="AccountUpdateEvent"/>, and that an event with a different <c>e</c> type deserializes to null
/// instead of throwing.
/// </summary>
public class AccountUpdateEventConverterTests : ProvidersTestBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AccountUpdateEventConverterTests"/> class.
    /// </summary>
    /// <param name="outputHelper">The xUnit output helper to route trace logging to.</param>
    public AccountUpdateEventConverterTests(ITestOutputHelper outputHelper)
        : base(outputHelper) { }

    /// <summary>
    /// Registers the Binance Spot provider so the converter under test is resolved from its actual registration.
    /// </summary>
    /// <param name="ctx">The fluent context to register providers into.</param>
    protected override void RegisterProvider(ProviderRegistrationContext ctx)
    {
        ctx.WithBinanceSpot();
    }

    /// <summary>
    /// A captured <c>outboundAccountPosition</c> event is parsed into its update time and the free/locked
    /// balance of each asset it lists.
    /// </summary>
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

    /// <summary>
    /// An event whose <c>e</c> type tag isn't <c>outboundAccountPosition</c> deserializes to null instead
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
        var serializer = this.GetJsonSerializer(Constants.AccountUpdateKey);
        var deserialized = serializer.Deserialize<AccountUpdateEvent>(Encoding.UTF8.GetBytes(raw));

        // assert - deserialization
        deserialized.IsDefault();
    }
}
