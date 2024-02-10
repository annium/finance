using System.Text;
using Annium.Finance.Providers.Abstractions.Domain.Models;
using Annium.Finance.Providers.Crypto.Binance.Base.Contracts.Market.Domain;
using Annium.Finance.Providers.Crypto.Binance.Spot.Internal.Contracts.Market.Domain;
using Annium.Finance.Providers.Tests.Shared.Connectors;
using Annium.Finance.Providers.Tests.Shared.Extensions;
using Annium.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Tests.Internal.Contracts.Market.Converters;

public class ExchangeInfoConverterTests : ConnectorTestBase
{
    public ExchangeInfoConverterTests(ITestOutputHelper outputHelper)
        : base(ctx => ctx.WithBinanceSpot(), outputHelper) { }

    [Fact]
    public void Works()
    {
        // arrange
        var raw =
            @"{
            ""timezone"": ""UTC"",
            ""serverTime"": 1690206335856,
            ""rateLimits"": [
                {
                    ""rateLimitType"": ""REQUEST_WEIGHT"",
                    ""interval"": ""MINUTE"",
                    ""intervalNum"": 1,
                    ""limit"": 6000
                },
                {
                    ""rateLimitType"": ""ORDERS"",
                    ""interval"": ""SECOND"",
                    ""intervalNum"": 10,
                    ""limit"": 50
                },
                {
                    ""rateLimitType"": ""ORDERS"",
                    ""interval"": ""DAY"",
                    ""intervalNum"": 1,
                    ""limit"": 160000
                },
                {
                    ""rateLimitType"": ""RAW_REQUESTS"",
                    ""interval"": ""MINUTE"",
                    ""intervalNum"": 5,
                    ""limit"": 6100
                }
            ],
            ""exchangeFilters"": [],
            ""symbols"": [
                {
                    ""symbol"": ""ETHBTC"",
                    ""status"": ""TRADING"",
                    ""baseAsset"": ""ETH"",
                    ""baseAssetPrecision"": 8,
                    ""quoteAsset"": ""BTC"",
                    ""quotePrecision"": 8,
                    ""quoteAssetPrecision"": 8,
                    ""baseCommissionPrecision"": 8,
                    ""quoteCommissionPrecision"": 8,
                    ""orderTypes"": [
                        ""LIMIT"",
                        ""LIMIT_MAKER"",
                        ""MARKET"",
                        ""STOP_LOSS_LIMIT"",
                        ""TAKE_PROFIT_LIMIT""
                    ],
                    ""icebergAllowed"": true,
                    ""ocoAllowed"": true,
                    ""quoteOrderQtyMarketAllowed"": false,
                    ""allowTrailingStop"": true,
                    ""cancelReplaceAllowed"": true,
                    ""isSpotTradingAllowed"": true,
                    ""isMarginTradingAllowed"": true,
                    ""filters"": [
                        {
                            ""filterType"": ""PRICE_FILTER"",
                            ""minPrice"": ""0.00001000"",
                            ""maxPrice"": ""922327.00000000"",
                            ""tickSize"": ""0.00001000""
                        },
                        {
                            ""filterType"": ""LOT_SIZE"",
                            ""minQty"": ""0.00010000"",
                            ""maxQty"": ""100000.00000000"",
                            ""stepSize"": ""0.00010000""
                        },
                        {
                            ""filterType"": ""ICEBERG_PARTS"",
                            ""limit"": 10
                        },
                        {
                            ""filterType"": ""MARKET_LOT_SIZE"",
                            ""minQty"": ""0.00000000"",
                            ""maxQty"": ""4623.26313000"",
                            ""stepSize"": ""0.00000000""
                        },
                        {
                            ""filterType"": ""TRAILING_DELTA"",
                            ""minTrailingAboveDelta"": 10,
                            ""maxTrailingAboveDelta"": 2000,
                            ""minTrailingBelowDelta"": 10,
                            ""maxTrailingBelowDelta"": 2000
                        },
                        {
                            ""filterType"": ""PERCENT_PRICE_BY_SIDE"",
                            ""bidMultiplierUp"": ""5"",
                            ""bidMultiplierDown"": ""0.2"",
                            ""askMultiplierUp"": ""5"",
                            ""askMultiplierDown"": ""0.2"",
                            ""avgPriceMins"": 5
                        },
                        {
                            ""filterType"": ""NOTIONAL"",
                            ""minNotional"": ""0.00010000"",
                            ""applyMinToMarket"": true,
                            ""maxNotional"": ""9000000.00000000"",
                            ""applyMaxToMarket"": false,
                            ""avgPriceMins"": 5
                        },
                        {
                            ""filterType"": ""MAX_NUM_ORDERS"",
                            ""maxNumOrders"": 200
                        },
                        {
                            ""filterType"": ""MAX_NUM_ALGO_ORDERS"",
                            ""maxNumAlgoOrders"": 5
                        }
                    ],
                    ""permissions"": [
                        ""SPOT"",
                        ""MARGIN"",
                        ""TRD_GRP_004"",
                        ""TRD_GRP_005"",
                        ""TRD_GRP_006"",
                        ""TRD_GRP_008"",
                        ""TRD_GRP_009"",
                        ""TRD_GRP_010"",
                        ""TRD_GRP_011"",
                        ""TRD_GRP_012"",
                        ""TRD_GRP_013""
                    ],
                    ""defaultSelfTradePreventionMode"": ""NONE"",
                    ""allowedSelfTradePreventionModes"": [
                        ""NONE"",
                        ""EXPIRE_TAKER"",
                        ""EXPIRE_MAKER"",
                        ""EXPIRE_BOTH""
                    ]
                }
            ]
        }";

        // act - deserialize
        var serializer = this.GetJsonSerializer(Constants.ExchangeInfoKey);
        var deserialized = serializer.Deserialize<ExchangeInfo?>(Encoding.UTF8.GetBytes(raw)).NotNull();

        // assert - deserialization
        deserialized.RateLimits.IsEqual(new RateLimits(6000));
        deserialized.Instruments.Has(1);
        var ethbtc = deserialized.Instruments.At(0);
        ethbtc.IsEqual(
            new InstrumentModel(
                "ETHBTC",
                new ResourceModel("ETH", 8),
                new ResourceModel("BTC", 8),
                new ResourceModel("BTC", 8),
                0.0001m,
                4623.26313m,
                0.0001m,
                0.00001m,
                922327m,
                0.00001m,
                0.0001m,
                9_000_000m,
                200
            )
        );
    }

    [Fact]
    public void SkipsInvalidData()
    {
        // arrange
        var raw =
            @"{
            ""s"": """"
        }";

        // act - deserialize
        var serializer = this.GetJsonSerializer(Constants.ExchangeInfoKey);
        var deserialized = serializer.Deserialize<ExchangeInfo?>(Encoding.UTF8.GetBytes(raw));

        // assert - deserialization
        deserialized.IsDefault();
    }
}
