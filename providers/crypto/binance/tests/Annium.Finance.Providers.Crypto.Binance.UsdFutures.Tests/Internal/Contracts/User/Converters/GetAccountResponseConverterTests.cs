using System.Linq;
using System.Text;
using Annium.Finance.Providers.Abstractions.Domain.Enums;
using Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Contracts.User.Domain;
using Annium.Finance.Providers.Tests.Shared.Connectors;
using Annium.Finance.Providers.Tests.Shared.Extensions;
using Annium.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Tests.Internal.Contracts.User.Converters;

public class GetAccountResponseConverterTests : ConnectorTestBase
{
    public GetAccountResponseConverterTests(ITestOutputHelper outputHelper)
        : base(ctx => ctx.WithBinanceUsdFutures(), outputHelper) { }

    [Fact]
    public void Works()
    {
        // arrange
        var raw =
            @"{
            ""feeTier"": 0,
            ""canTrade"": true,
            ""canDeposit"": true,
            ""canWithdraw"": true,
            ""updateTime"": 0,
            ""multiAssetsMargin"": false,
            ""totalInitialMargin"": ""0.00000000"",
            ""totalMaintMargin"": ""0.00000000"",
            ""totalWalletBalance"": ""23.72469206"",
            ""totalUnrealizedProfit"": ""0.00000000"",
            ""totalMarginBalance"": ""23.72469206"",
            ""totalPositionInitialMargin"": ""0.00000000"",
            ""totalOpenOrderInitialMargin"": ""0.00000000"",
            ""totalCrossWalletBalance"": ""23.72469206"",
            ""totalCrossUnPnl"": ""0.00000000"",
            ""availableBalance"": ""23.72469206"",
            ""maxWithdrawAmount"": ""23.72469206"",
            ""assets"": [
                {
                    ""asset"": ""USDT"",
                    ""walletBalance"": ""23.72469206"",
                    ""unrealizedProfit"": ""-2.60000000"",
                    ""marginBalance"": ""10.7"",
                    ""maintMargin"": ""2.30000000"",
                    ""initialMargin"": ""4.40000000"",
                    ""positionInitialMargin"": ""0.00000000"",
                    ""openOrderInitialMargin"": ""0.00000000"",
                    ""crossWalletBalance"": ""23.72469206"",
                    ""crossUnPnl"": ""0.00000000"",
                    ""availableBalance"": ""2.9"",
                    ""maxWithdrawAmount"": ""1.7"",
                    ""marginAvailable"": true,
                    ""updateTime"": 1625474304765
                },
                {
                    ""asset"": ""BUSD"",
                    ""walletBalance"": ""103.12345678"",
                    ""unrealizedProfit"": ""5.10000000"",
                    ""marginBalance"": ""30.2"",
                    ""maintMargin"": ""7.2"",
                    ""initialMargin"": ""1.5"",
                    ""positionInitialMargin"": ""0.00000000"",
                    ""openOrderInitialMargin"": ""0.00000000"",
                    ""crossWalletBalance"": ""103.12345678"",
                    ""crossUnPnl"": ""0.00000000"",
                    ""availableBalance"": ""4.5"",
                    ""maxWithdrawAmount"": ""3.2"",
                    ""marginAvailable"": true,
                    ""updateTime"": 1625474304766
                }
            ],
            ""positions"": [
                {
                    ""symbol"": ""BTCUSDT"",
                    ""initialMargin"": ""0"",
                    ""maintMargin"": ""0"",
                    ""unrealizedProfit"": ""2123123.60000000"",
                    ""positionInitialMargin"": ""0"",
                    ""openOrderInitialMargin"": ""0"",
                    ""leverage"": ""100"",
                    ""isolated"": true,
                    ""entryPrice"": ""9974.3"",
                    ""maxNotional"": ""250000"",
                    ""bidNotional"": ""0"",
                    ""askNotional"": ""0"",
                    ""positionSide"": ""LONG"",
                    ""positionAmt"": ""7.5"",
                    ""updateTime"": 1625474304764
                }
            ]
        }";

        // act - deserialize
        var serializer = this.GetJsonSerializer(Constants.GetAccountKey);
        var deserialized = serializer.Deserialize<AccountResponse>(Encoding.UTF8.GetBytes(raw)).NotNull();

        // assert - deserialization
        deserialized.Balances.Has(2);
        deserialized
            .Balances.ElementAt(0)
            .IsEqual(new AccountResponseBalance("USDT", 10.7m, 1.7m, 4.4m, 2.3m, 1625474304765));
        deserialized
            .Balances.ElementAt(1)
            .IsEqual(new AccountResponseBalance("BUSD", 30.2m, 3.2m, 1.5m, 7.2m, 1625474304766));
        deserialized.Positions.Has(1);
        deserialized
            .Positions.ElementAt(0)
            .IsEqual(
                new AccountResponsePosition(
                    "BTCUSDT",
                    OrientationRange.Long,
                    MarginType.Isolated,
                    100,
                    7.5m,
                    9974.3m,
                    2123123.6m,
                    1625474304764
                )
            );
    }
}
