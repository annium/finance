using System.Text;
using Annium.Finance.Providers.Core;
using Annium.Finance.Providers.Crypto.Binance.Base.Shared.Contracts.Domain;
using Annium.Finance.Providers.Tests.Lib;
using Annium.Finance.Providers.Tests.Lib.Infrastructure;
using Annium.Testing;
using Xunit;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Tests.Internal.User.Contracts.Converters;

/// <summary>
/// Verifies that the converter reads Binance's <c>POST /order/cancelReplace</c> failure envelope - which
/// nests the actual cancel and new-order outcomes under <c>data</c> - and surfaces whichever leg's error
/// is the relevant one, as an <see cref="OperationResult"/>.
/// </summary>
public class ModifyOrderFailureResponseConverterTests : ProvidersTestBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ModifyOrderFailureResponseConverterTests"/> class.
    /// </summary>
    /// <param name="outputHelper">The xUnit output helper to route trace logging to.</param>
    public ModifyOrderFailureResponseConverterTests(ITestOutputHelper outputHelper)
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
    /// When the cancel leg itself failed (cancel-replace stops there, the new order is never attempted),
    /// the nested cancel error's code and message are surfaced.
    /// </summary>
    [Fact]
    public void StopOnFailure()
    {
        // arrange
        var raw =
            @"{
            ""code"": -2022,
            ""msg"": ""Order cancel-replace failed."",
            ""data"": {
                ""cancelResult"": ""FAILURE"",
                ""newOrderResult"": ""NOT_ATTEMPTED"",
                ""cancelResponse"": {
                    ""code"": -2011,
                    ""msg"": ""Unknown order sent.""
                },
                ""newOrderResponse"": null
            }
        }";

        // act
        var serializer = this.GetJsonSerializer(Constants.ModifyOrderKey);
        var deserialized = serializer.Deserialize<OperationResult>(Encoding.UTF8.GetBytes(raw)).NotNull();

        // assert
        deserialized.Code.Is(-2011);
        deserialized.Message.Is("Unknown order sent.");
    }

    /// <summary>
    /// When the cancel leg succeeds but placing the replacement order fails, the nested new-order error's
    /// code and message are surfaced instead of the cancel's.
    /// </summary>
    [Fact]
    public void NewOrderFails()
    {
        // arrange
        var raw =
            @"{
            ""code"": -2021,
            ""msg"": ""Order cancel-replace partially failed."",
            ""data"": {
                ""cancelResult"": ""SUCCESS"",
                ""newOrderResult"": ""FAILURE"",
                ""cancelResponse"": {
                    ""symbol"": ""BTCUSDT"",
                    ""origClientOrderId"": ""86M8erehfExV8z2RC8Zo8k"",
                    ""orderId"": 3,
                    ""orderListId"": -1,
                    ""clientOrderId"": ""G1kLo6aDv2KGNTFcjfTSFq"",
                    ""transactTime"": 1684804350068,
                    ""price"": ""0.006123"",
                    ""origQty"": ""10000.000000"",
                    ""executedQty"": ""0.000000"",
                    ""cummulativeQuoteQty"": ""0.000000"",
                    ""status"": ""CANCELED"",
                    ""timeInForce"": ""GTC"",
                    ""type"": ""LIMIT_MAKER"",
                    ""side"": ""SELL"",
                    ""selfTradePreventionMode"": ""NONE""
                },
                ""newOrderResponse"": {
                    ""code"": -2010,
                    ""msg"": ""Order would immediately match and take.""
                }
            }
        }";

        // act
        var serializer = this.GetJsonSerializer(Constants.ModifyOrderKey);
        var deserialized = serializer.Deserialize<OperationResult>(Encoding.UTF8.GetBytes(raw)).NotNull();

        // assert
        deserialized.Code.Is(-2010);
        deserialized.Message.Is("Order would immediately match and take.");
    }
}
