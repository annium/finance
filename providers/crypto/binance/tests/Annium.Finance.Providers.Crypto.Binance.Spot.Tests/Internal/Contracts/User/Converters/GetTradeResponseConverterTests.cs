using System.Text;
using Annium.Finance.Providers.Crypto.Binance.Spot.Internal.Contracts.User.Domain;
using Annium.Finance.Providers.Tests.Shared.Connectors;
using Annium.Finance.Providers.Tests.Shared.Extensions;
using Annium.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Tests.Internal.Contracts.User.Converters;

public class GetTradeResponseConverterTests : ConnectorTestBase
{
    public GetTradeResponseConverterTests(ITestOutputHelper outputHelper)
        : base(ctx => ctx.WithBinanceSpot(), outputHelper) { }

    [Fact]
    public void Works()
    {
        // arrange
        var raw =
            @"{
            ""symbol"": ""BNBBTC"",
            ""id"": 28457,
            ""orderId"": 100234,
            ""orderListId"": -1,
            ""price"": ""4.00000100"",
            ""qty"": ""12.00070000"",
            ""quoteQty"": ""48.000012"",
            ""commission"": ""10.10000000"",
            ""commissionAsset"": ""BNB"",
            ""time"": 1499865549590,
            ""isBuyer"": true,
            ""isMaker"": true,
            ""isBestMatch"": true
        }";

        // act
        var serializer = this.GetJsonSerializer(Constants.GetTrade);
        var deserialized = serializer.Deserialize<TradeResponse>(Encoding.UTF8.GetBytes(raw)).NotNull();

        // assert
        deserialized.OrderId.Is("100234");
        deserialized.Symbol.Is("BNBBTC");
        deserialized.Price.Is(4.000001m);
        deserialized.Qty.Is(12.0007m);
        deserialized.Commission.Is(10.1m);
        deserialized.CommissionAsset.Is("BNB");
        deserialized.Maker.IsTrue();
        deserialized.Moment.Is(1499865549590);
    }
}
