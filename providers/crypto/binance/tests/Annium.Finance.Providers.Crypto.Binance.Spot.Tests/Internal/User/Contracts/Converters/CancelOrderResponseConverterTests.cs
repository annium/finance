using System;
using System.Text;
using Annium.Finance.Providers.Core;
using Annium.Finance.Providers.Crypto.Binance.Base.User.Contracts.Domain;
using Annium.Finance.Providers.Tests.Lib;
using Annium.Finance.Providers.Tests.Lib.Infrastructure;
using Annium.Testing;
using Xunit;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Tests.Internal.User.Contracts.Converters;

/// <summary>
/// Verifies that <c>CancelOrderResponseConverter</c> reads Binance's <c>DELETE /order</c> response into a
/// <see cref="CancelOrderResponse"/>, notably parsing the returned <c>clientOrderId</c> - which Binance
/// generates fresh for the cancel confirmation - as a <see cref="Guid"/>.
/// </summary>
public class CancelOrderResponseConverterTests : ProvidersTestBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CancelOrderResponseConverterTests"/> class.
    /// </summary>
    /// <param name="outputHelper">The xUnit output helper to route trace logging to.</param>
    public CancelOrderResponseConverterTests(ITestOutputHelper outputHelper)
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
    /// A captured cancel-order response is parsed into the numeric order id and the client order id, the
    /// latter carried as a GUID.
    /// </summary>
    [Fact]
    public void Success()
    {
        // arrange
        var raw =
            @"{
            ""symbol"": ""LTCBTC"",
            ""origClientOrderId"": ""myOrder1"",
            ""orderId"": 4,
            ""orderListId"": -1,
            ""clientOrderId"": ""c5a7733c-ad87-4c6d-a2a4-941ee8e0f7f7"",
            ""transactTime"": 1684804350068,
            ""price"": ""2.00000000"",
            ""origQty"": ""1.00000000"",
            ""executedQty"": ""0.00000000"",
            ""cummulativeQuoteQty"": ""0.00000000"",
            ""status"": ""CANCELED"",
            ""timeInForce"": ""GTC"",
            ""type"": ""LIMIT"",
            ""side"": ""BUY"",
            ""selfTradePreventionMode"": ""NONE""
        }";

        // act
        var serializer = this.GetJsonSerializer(Constants.CancelOrderKey);
        var deserialized = serializer.Deserialize<CancelOrderResponse>(Encoding.UTF8.GetBytes(raw)).NotNull();

        // assert
        deserialized.OrderId.Is("4");
        deserialized.Id.Is(Guid.Parse("c5a7733c-ad87-4c6d-a2a4-941ee8e0f7f7"));
    }
}
