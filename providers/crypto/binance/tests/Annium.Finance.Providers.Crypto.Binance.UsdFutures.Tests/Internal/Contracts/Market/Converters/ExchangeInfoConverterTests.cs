using System.Text;
using Annium.Finance.Providers.Abstractions.Domain.Models;
using Annium.Finance.Providers.Crypto.Binance.Base.Contracts.Market.Domain;
using Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Contracts.Market.Domain;
using Annium.Finance.Providers.Tests.Lib;
using Annium.Finance.Providers.Tests.Lib.Extensions;
using Annium.Testing;
using Xunit;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Tests.Internal.Contracts.Market.Converters;

public class ExchangeInfoConverterTests : ProvidersTestBase
{
    public ExchangeInfoConverterTests(ITestOutputHelper outputHelper)
        : base(ctx => ctx.WithBinanceUsdFutures(), outputHelper) { }

    [Fact]
    public void Works()
    {
        // arrange
        var raw =
            @"{
            ""timezone"": ""UTC"",
            ""serverTime"": 1690214411331,
            ""futuresType"": ""U_MARGINED"",
            ""rateLimits"": [
                {
                    ""rateLimitType"": ""REQUEST_WEIGHT"",
                    ""interval"": ""MINUTE"",
                    ""intervalNum"": 1,
                    ""limit"": 2400
                },
                {
                    ""rateLimitType"": ""ORDERS"",
                    ""interval"": ""MINUTE"",
                    ""intervalNum"": 1,
                    ""limit"": 1200
                },
                {
                    ""rateLimitType"": ""ORDERS"",
                    ""interval"": ""SECOND"",
                    ""intervalNum"": 10,
                    ""limit"": 300
                }
            ],
            ""exchangeFilters"": [],
            ""assets"": [
                {
                    ""asset"": ""USDT"",
                    ""marginAvailable"": true,
                    ""autoAssetExchange"": ""-10000""
                },
                {
                  ""asset"": ""ETH"",
                  ""marginAvailable"": true,
                  ""autoAssetExchange"": ""-5""
                }
            ],
            ""symbols"": [
                {
                    ""symbol"": ""BTCUSDT"",
                    ""pair"": ""BTCUSDT"",
                    ""contractType"": ""PERPETUAL"",
                    ""deliveryDate"": 4133404800000,
                    ""onboardDate"": 1569398400000,
                    ""status"": ""TRADING"",
                    ""maintMarginPercent"": ""2.5000"",
                    ""requiredMarginPercent"": ""5.0000"",
                    ""baseAsset"": ""BTC"",
                    ""quoteAsset"": ""USDT"",
                    ""marginAsset"": ""USDT"",
                    ""pricePrecision"": 2,
                    ""quantityPrecision"": 3,
                    ""baseAssetPrecision"": 8,
                    ""quotePrecision"": 8,
                    ""underlyingType"": ""COIN"",
                    ""underlyingSubType"": [
                        ""PoW""
                    ],
                    ""settlePlan"": 0,
                    ""triggerProtect"": ""0.0500"",
                    ""liquidationFee"": ""0.012500"",
                    ""marketTakeBound"": ""0.05"",
                    ""maxMoveOrderLimit"": 10000,
                    ""filters"": [
                        {
                            ""minPrice"": ""556.80"",
                            ""maxPrice"": ""4529764"",
                            ""filterType"": ""PRICE_FILTER"",
                            ""tickSize"": ""0.10""
                        },
                        {
                            ""stepSize"": ""0.001"",
                            ""filterType"": ""LOT_SIZE"",
                            ""maxQty"": ""1000"",
                            ""minQty"": ""0.001""
                        },
                        {
                            ""stepSize"": ""0.001"",
                            ""filterType"": ""MARKET_LOT_SIZE"",
                            ""maxQty"": ""120"",
                            ""minQty"": ""0.001""
                        },
                        {
                            ""limit"": 200,
                            ""filterType"": ""MAX_NUM_ORDERS""
                        },
                        {
                            ""limit"": 10,
                            ""filterType"": ""MAX_NUM_ALGO_ORDERS""
                        },
                        {
                            ""notional"": ""5.0"",
                            ""filterType"": ""MIN_NOTIONAL""
                        },
                        {
                            ""multiplierDown"": ""0.9500"",
                            ""multiplierUp"": ""1.0500"",
                            ""multiplierDecimal"": ""4"",
                            ""filterType"": ""PERCENT_PRICE""
                        }
                    ],
                    ""orderTypes"": [
                        ""LIMIT"",
                        ""MARKET"",
                        ""STOP"",
                        ""STOP_MARKET"",
                        ""TAKE_PROFIT"",
                        ""TAKE_PROFIT_MARKET"",
                        ""TRAILING_STOP_MARKET""
                    ],
                    ""timeInForce"": [
                        ""GTC"",
                        ""IOC"",
                        ""FOK"",
                        ""GTX""
                    ]
                }
            ]
        }";

        // act - deserialize
        var serializer = this.GetJsonSerializer(Constants.ExchangeInfoKey);
        var deserialized = serializer.Deserialize<ExchangeInfo?>(Encoding.UTF8.GetBytes(raw)).NotNull();

        // assert - deserialization
        deserialized.RateLimits.IsEqual(new RateLimits(2400));
        deserialized.Assets.Has(2);
        deserialized.Assets.At(0).Code.Is("USDT");
        deserialized.Assets.At(1).Code.Is("ETH");
        deserialized.Instruments.Has(1);
        var btcusdt = deserialized.Instruments.At(0);
        btcusdt.IsEqual(
            new InstrumentModel(
                "BTCUSDT",
                new ResourceModel("BTC", 8),
                new ResourceModel("USDT", 8),
                new ResourceModel("USDT", 8),
                0.001m,
                120m,
                0.001m,
                556.8m,
                4529764m,
                0.1m,
                5m,
                decimal.MaxValue,
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
