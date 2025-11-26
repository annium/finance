using System.Text;
using Annium.Finance.Providers.Abstractions.Domain.Models;
using Annium.Finance.Providers.Shared;
using Annium.Finance.Providers.Tests.Lib;
using Annium.Finance.Providers.Tests.Lib.TestBaseExtensions;
using Annium.Testing;
using Xunit;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Tests.Internal.Contracts.User.Converters;

public class GetTradeResponseConverterTests : ProvidersTestBase
{
    public GetTradeResponseConverterTests(ITestOutputHelper outputHelper)
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
        var serializer = this.GetJsonSerializer(Constants.GetTradeKey);
        var deserialized = serializer.Deserialize<TradeModel>(Encoding.UTF8.GetBytes(raw)).NotNull();

        // assert
        deserialized.Id.Is("28457");
        deserialized.OrderId.Is("100234");
        deserialized.Symbol.Is("BNBBTC");
        deserialized.Price.Is(4.000001m);
        deserialized.Qty.Is(12.0007m);
        deserialized.CommissionAsset.Is("BNB");
        deserialized.CommissionAmount.Is(10.1m);
        deserialized.Maker.IsTrue();
        deserialized.Moment.Is(1499865549590);
    }
}
