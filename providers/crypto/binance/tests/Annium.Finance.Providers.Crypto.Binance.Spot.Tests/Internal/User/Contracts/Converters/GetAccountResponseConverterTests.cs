using System.Collections.Generic;
using System.Text;
using Annium.Finance.Providers.Abstractions.Domain.User;
using Annium.Finance.Providers.Core;
using Annium.Finance.Providers.Tests.Lib;
using Annium.Finance.Providers.Tests.Lib.Infrastructure;
using Annium.Testing;
using Xunit;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Tests.Internal.User.Contracts.Converters;

/// <summary>
/// Verifies that the converter reads Binance's <c>GET /account</c> response - commission rates, trading
/// flags, and a per-asset free/locked balance list - down to just the balances, mapped to
/// <see cref="AssetModel"/>.
/// </summary>
public class GetAccountResponseConverterTests : ProvidersTestBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetAccountResponseConverterTests"/> class.
    /// </summary>
    /// <param name="outputHelper">The xUnit output helper to route trace logging to.</param>
    public GetAccountResponseConverterTests(ITestOutputHelper outputHelper)
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
    /// A captured account response is parsed into its per-asset balances, discarding the surrounding
    /// commission and permission data the converter doesn't need.
    /// </summary>
    [Fact]
    public void Works()
    {
        // arrange
        var raw =
            @"{
            ""makerCommission"": 15,
            ""takerCommission"": 15,
            ""buyerCommission"": 0,
            ""sellerCommission"": 0,
            ""commissionRates"": {
                ""maker"": ""0.00150000"",
                ""taker"": ""0.00150000"",
                ""buyer"": ""0.00000000"",
                ""seller"": ""0.00000000""
            },
            ""canTrade"": true,
            ""canWithdraw"": true,
            ""canDeposit"": true,
            ""brokered"": false,
            ""requireSelfTradePrevention"": false,
            ""preventSor"": false,
            ""updateTime"": 123456789,
            ""accountType"": ""SPOT"",
            ""balances"": [
                {
                    ""asset"": ""BTC"",
                    ""free"": ""1.2"",
                    ""locked"": ""2.3""
                },
                {
                    ""asset"": ""LTC"",
                    ""free"": ""1.3"",
                    ""locked"": ""2.4""
                }
            ],
            ""permissions"": [
                ""SPOT""
            ],
            ""uid"": 354937868
        }";

        // act - deserialize
        var serializer = this.GetJsonSerializer(Constants.GetAccountKey);
        var deserialized = serializer
            .Deserialize<IReadOnlyCollection<AssetModel>>(Encoding.UTF8.GetBytes(raw))
            .NotNull();

        // assert - deserialization
        deserialized.Has(2);
        deserialized.At(0).IsEqual(new AssetModel("BTC", 1.2m, 2.3m));
        deserialized.At(1).IsEqual(new AssetModel("LTC", 1.3m, 2.4m));
    }
}
