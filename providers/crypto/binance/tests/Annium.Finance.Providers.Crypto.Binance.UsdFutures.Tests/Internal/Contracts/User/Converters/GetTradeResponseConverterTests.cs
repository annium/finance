using System.Text;
using Annium.Finance.Providers.Abstractions.Domain.Models;
using Annium.Finance.Providers.Tests.Shared.Connectors;
using Annium.Finance.Providers.Tests.Shared.Extensions;
using Annium.Testing;
using Xunit;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Tests.Internal.Contracts.User.Converters;

public class GetTradeResponseConverterTests : ConnectorTestBase
{
    public GetTradeResponseConverterTests(ITestOutputHelper outputHelper)
        : base(ctx => ctx.WithBinanceUsdFutures(), outputHelper) { }

    [Fact]
    public void Works()
    {
        // arrange
        var raw =
            @"{
            ""buyer"": false,
            ""commission"": ""-0.07819010"",
            ""commissionAsset"": ""USDT"",
            ""id"": 698759,
            ""maker"": true,
            ""orderId"": 25851813,
            ""price"": ""7819.01"",
            ""qty"": ""0.002"",
            ""quoteQty"": ""15.63802"",
            ""realizedPnl"": ""-0.91539999"",
            ""side"": ""SELL"",
            ""positionSide"": ""SHORT"",
            ""symbol"": ""BTCUSDT"",
            ""time"": 1569514978020
        }";

        // act
        var serializer = this.GetJsonSerializer(Constants.GetTradeKey);
        var deserialized = serializer.Deserialize<TradeModel>(Encoding.UTF8.GetBytes(raw)).NotNull();

        // assert
        deserialized.Id.Is("698759");
        deserialized.OrderId.Is("25851813");
        deserialized.Symbol.Is("BTCUSDT");
        deserialized.Price.Is(7819.01m);
        deserialized.Qty.Is(0.002m);
        deserialized.CommissionAsset.Is("USDT");
        deserialized.CommissionAmount.Is(-0.07819010m);
        deserialized.Maker.IsTrue();
        deserialized.Moment.Is(1569514978020);
    }
}
