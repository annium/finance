using System.Text;
using Annium.Finance.Providers.Crypto.Binance.Base.Contracts.Shared.Domain;
using Annium.Finance.Providers.Tests.Shared.Connectors;
using Annium.Finance.Providers.Tests.Shared.Extensions;
using Annium.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Tests.Internal.Contracts.User.Converters;

public class ModifyOrderFailureResponseConverterTests : ConnectorTestBase
{
    public ModifyOrderFailureResponseConverterTests(ITestOutputHelper outputHelper)
        : base(ctx => ctx.WithBinanceSpot(), outputHelper) { }

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
