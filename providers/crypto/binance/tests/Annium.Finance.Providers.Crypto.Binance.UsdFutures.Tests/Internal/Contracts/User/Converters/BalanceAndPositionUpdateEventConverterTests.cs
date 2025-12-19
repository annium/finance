using System.Text;
using Annium.Finance.Providers.Abstractions.Domain.User;
using Annium.Finance.Providers.Core;
using Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.User.Contracts.Domain;
using Annium.Finance.Providers.Tests.Lib;
using Annium.Finance.Providers.Tests.Lib.Infrastructure;
using Annium.Testing;
using Xunit;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Tests.Internal.Contracts.User.Converters;

public class BalanceAndPositionUpdateEventConverterTests : ProvidersTestBase
{
    public BalanceAndPositionUpdateEventConverterTests(ITestOutputHelper outputHelper)
        : base(outputHelper) { }

    protected override void RegisterProvider(ProviderRegistrationContext ctx)
    {
        ctx.WithBinanceUsdFutures();
    }

    [Fact]
    public void Works()
    {
        // arrange
        var raw =
            @"{
            ""e"": ""ACCOUNT_UPDATE"",
            ""E"": 1564745798939,
            ""T"": 1564745798938 ,
            ""a"": {
                ""m"":""ORDER"",
                ""B"":[
                    {
                        ""a"":""USDT"",
                        ""wb"":""122624.123"",
                        ""cw"":""100.123"",
                        ""bc"":""50.123""
                    },
                    {
                        ""a"":""BUSD"",
                        ""wb"":""1.00000000"",
                        ""cw"":""0.00000000"",
                        ""bc"":""-49.123""
                    }
                ],
                ""P"":[
                    {
                        ""s"":""BTCUSDT"",
                        ""pa"":""0"",
                        ""ep"":""0.00000"",
                        ""cr"":""200"",
                        ""up"":""0"",
                        ""mt"":""cross"",
                        ""iw"":""0.00000000"",
                        ""ps"":""BOTH""
                    },
                    {
                        ""s"":""ETHUSDT"",
                        ""pa"":""20"",
                        ""ep"":""6563.66500"",
                        ""cr"":""0"",
                        ""up"":""2850.21200"",
                        ""mt"":""isolated"",
                        ""iw"":""13200.1"",
                        ""ps"":""LONG""
                    },
                    {
                        ""s"":""LTCUSDT"",
                        ""pa"":""-10"",
                        ""ep"":""6563.86000"",
                        ""cr"":""-45.04000000"",
                        ""up"":""-1423.15600"",
                        ""mt"":""isolated"",
                        ""iw"":""6570.2"",
                        ""ps"":""SHORT""
                    }
                ]
            }
        }";

        // act - deserialize
        var serializer = this.GetJsonSerializer(Constants.BalanceAndPositionUpdateKey);
        var deserialized = serializer.Deserialize<BalanceAndPositionUpdateEvent>(Encoding.UTF8.GetBytes(raw)).NotNull();

        // assert - deserialization
        deserialized.Date.Is(1564745798938);
        deserialized.Balances.Has(2);
        deserialized
            .Balances.At(0)
            .IsEqual(new BalanceAndPositionUpdateEventBalance("USDT", 122624.123m, 100.123m, 50.123m));
        deserialized.Balances.At(1).IsEqual(new BalanceAndPositionUpdateEventBalance("BUSD", 1, 0, -49.123m));
        deserialized.Positions.Has(3);
        deserialized
            .Positions.At(0)
            .IsEqual(
                new BalanceAndPositionUpdateEventPosition(
                    "BTCUSDT",
                    OrientationRange.Both,
                    MarginType.Cross,
                    0,
                    0,
                    0,
                    0
                )
            );
        deserialized
            .Positions.At(1)
            .IsEqual(
                new BalanceAndPositionUpdateEventPosition(
                    "ETHUSDT",
                    OrientationRange.Long,
                    MarginType.Isolated,
                    13200.1m,
                    20,
                    6563.665m,
                    2850.212m
                )
            );
        deserialized
            .Positions.At(2)
            .IsEqual(
                new BalanceAndPositionUpdateEventPosition(
                    "LTCUSDT",
                    OrientationRange.Short,
                    MarginType.Isolated,
                    6570.2m,
                    -10,
                    6563.86m,
                    -1423.156m
                )
            );
    }

    [Fact]
    public void SkipsInvalidData()
    {
        // arrange
        var raw =
            @"{
            ""e"": ""invalid""
        }";

        // act - deserialize
        var serializer = this.GetJsonSerializer(Constants.BalanceAndPositionUpdateKey);
        var deserialized = serializer.Deserialize<BalanceAndPositionUpdateEvent>(Encoding.UTF8.GetBytes(raw));

        // assert - deserialization
        deserialized.IsDefault();
    }
}
