using System;
using System.Collections.Generic;
using System.Globalization;
using Annium.Finance.Providers.Abstractions.Domain.User;
using Annium.Finance.Providers.Abstractions.Domain.User.Requests;
using Annium.Finance.Providers.Core;
using Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Connectors;
using Annium.Finance.Providers.Tests.Lib;
using Annium.Finance.Providers.Tests.Lib.User.Operations;
using Annium.Finance.Providers.Tests.Lib.User.Requests;
using Annium.Testing;
using Xunit;
using static Annium.Finance.Providers.Abstractions.Domain.User.Requests.RequestBuilder;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Tests.Internal.Connectors;

public class QueryProcessorTests : ProvidersTestBase
{
    private const string Symbol = "BTCUSDT";
    private static readonly string _clientOrderId = Guid.NewGuid().ToString();
    private static readonly OrientationRange _range = OrientationRange.Both;

    public QueryProcessorTests(ITestOutputHelper outputHelper)
        : base(outputHelper) { }

    protected override void RegisterProvider(ProviderRegistrationContext ctx)
    {
        ctx.WithBinanceUsdFutures();
    }

    [Theory]
    [InlineData(9, false)]
    [InlineData(10, true)]
    public void InitOrder_Limit(int count, bool reduceOnly)
    {
        // arrange
        var processor = Get<QueryProcessor>();
        var request = InitLimitOrder(_clientOrderId, _range, Symbol, OrderSide.Buy, 10.5m, 15.2m, reduceOnly);

        // act
        var data = processor.BuildInitOrderQuery(request).Unwrap().As<IReadOnlyDictionary<string, string>>();

        // assert
        data.Has(count);
        data.At("newClientOrderId").Is(request.Id);
        data.At("symbol").Is(request.Symbol);
        data.At("side").Is("BUY");
        data.At("positionSide").Is("BOTH");
        data.At("type").Is("LIMIT");
        data.At("timeInForce").Is("GTC");
        data.At("newOrderRespType").Is("RESULT");
        data.At("quantity").Is("10.5");
        data.At("price").Is("15.2");

        if (reduceOnly)
            data.At("reduceOnly").Is("true");
    }

    [Theory]
    [InlineData(7, false)]
    [InlineData(8, true)]
    public void InitOrder_Market(int count, bool reduceOnly)
    {
        // arrange
        var processor = Get<QueryProcessor>();
        var request = InitMarketOrder(_clientOrderId, _range, Symbol, OrderSide.Buy, 10.5m, reduceOnly);

        // act
        var data = processor.BuildInitOrderQuery(request).Unwrap().As<IReadOnlyDictionary<string, string>>();

        // assert
        data.Has(count);
        data.At("newClientOrderId").Is(request.Id);
        data.At("symbol").Is(request.Symbol);
        data.At("side").Is("BUY");
        data.At("positionSide").Is("BOTH");
        data.At("type").Is("MARKET");
        data.At("newOrderRespType").Is("RESULT");
        data.At("quantity").Is("10.5");

        if (reduceOnly)
            data.At("reduceOnly").Is("true");
    }

    [Theory]
    [InlineData(8, 10.5, false)]
    [InlineData(9, 10.5, true)]
    public void InitOrder_StopLossMarket(int count, decimal quantity, bool reduceOnly)
    {
        // arrange
        var processor = Get<QueryProcessor>();
        var request = InitStopLossMarketOrder(
            _clientOrderId,
            _range,
            Symbol,
            OrderSide.Sell,
            quantity,
            9.4m,
            reduceOnly
        );

        // act
        var data = processor.BuildInitOrderQuery(request).Unwrap().As<IReadOnlyDictionary<string, string>>();

        // assert
        data.Has(count);
        data.At("newClientOrderId").Is(request.Id);
        data.At("symbol").Is(request.Symbol);
        data.At("side").Is("SELL");
        data.At("positionSide").Is("BOTH");
        data.At("type").Is("STOP_MARKET");
        data.At("newOrderRespType").Is("RESULT");
        data.At("stopPrice").Is("9.4");

        data.At("quantity").Is(quantity.ToString(CultureInfo.InvariantCulture));
        if (reduceOnly)
            data.At("reduceOnly").Is("true");
    }

    [Theory]
    [InlineData(8, 10.5, false)]
    [InlineData(9, 10.5, true)]
    public void InitOrder_TakeProfitMarket(int count, decimal quantity, bool reduceOnly)
    {
        // arrange
        var processor = Get<QueryProcessor>();
        var request = InitTakeProfitMarketOrder(
            _clientOrderId,
            _range,
            Symbol,
            OrderSide.Sell,
            quantity,
            9.4m,
            reduceOnly
        );

        // act
        var data = processor.BuildInitOrderQuery(request).Unwrap().As<IReadOnlyDictionary<string, string>>();

        // assert
        data.Has(count);
        data.At("newClientOrderId").Is(request.Id);
        data.At("symbol").Is(request.Symbol);
        data.At("side").Is("SELL");
        data.At("positionSide").Is("BOTH");
        data.At("type").Is("TAKE_PROFIT_MARKET");
        data.At("newOrderRespType").Is("RESULT");
        data.At("stopPrice").Is("9.4");

        data.At("quantity").Is(quantity.ToString(CultureInfo.InvariantCulture));
        if (reduceOnly)
            data.At("reduceOnly").Is("true");
    }

    [Theory]
    [InlineData(10, false)]
    [InlineData(11, true)]
    public void InitOrder_StopLossLimit(int count, bool reduceOnly)
    {
        // arrange
        var processor = Get<QueryProcessor>();
        var request = InitStopLossLimitOrder(
            _clientOrderId,
            _range,
            Symbol,
            OrderSide.Sell,
            10.5m,
            9.4m,
            9.6m,
            reduceOnly
        );

        // act
        var data = processor.BuildInitOrderQuery(request).Unwrap().As<IReadOnlyDictionary<string, string>>();

        // assert
        data.Has(count);
        data.At("newClientOrderId").Is(request.Id);
        data.At("symbol").Is(request.Symbol);
        data.At("side").Is("SELL");
        data.At("positionSide").Is("BOTH");
        data.At("type").Is("STOP");
        data.At("timeInForce").Is("GTC");
        data.At("newOrderRespType").Is("RESULT");
        data.At("quantity").Is("10.5");
        data.At("price").Is("9.4");
        data.At("stopPrice").Is("9.6");

        if (reduceOnly)
            data.At("reduceOnly").Is("true");
    }

    [Theory]
    [InlineData(10, false)]
    [InlineData(11, true)]
    public void InitOrder_TakeProfitLimit(int count, bool reduceOnly)
    {
        // arrange
        var processor = Get<QueryProcessor>();
        var request = InitTakeProfitLimitOrder(
            _clientOrderId,
            _range,
            Symbol,
            OrderSide.Sell,
            10.5m,
            9.4m,
            9.2m,
            reduceOnly
        );

        // act
        var data = processor.BuildInitOrderQuery(request).Unwrap().As<IReadOnlyDictionary<string, string>>();

        // assert
        data.Has(count);
        data.At("newClientOrderId").Is(request.Id);
        data.At("symbol").Is(request.Symbol);
        data.At("side").Is("SELL");
        data.At("positionSide").Is("BOTH");
        data.At("type").Is("TAKE_PROFIT");
        data.At("timeInForce").Is("GTC");
        data.At("newOrderRespType").Is("RESULT");
        data.At("quantity").Is("10.5");
        data.At("price").Is("9.4");
        data.At("stopPrice").Is("9.2");

        if (reduceOnly)
            data.At("reduceOnly").Is("true");
    }

    [Fact]
    public void ModifyOrder_Limit()
    {
        // arrange
        var processor = Get<QueryProcessor>();
        var order = InitLimitOrder(_clientOrderId, _range, Symbol, OrderSide.Buy, 10.5m, 15.2m).ToOrder();
        var request = ModifyToLimitOrder(order, OrderSide.Buy, 11.3m, 12.7m);

        // act
        var data = processor.BuildModifyOrderQuery(request).Unwrap().As<IReadOnlyDictionary<string, string>>();

        // assert
        data.Has(5);
        data.At("symbol").Is(request.Order.Symbol);
        data.At("side").Is("BUY");
        data.At("quantity").Is("11.3");
        data.At("price").Is("12.7");
        data.At("origClientOrderId").Is(request.Order.ClientOrderId);
    }

    [Fact]
    public void ModifyOrder_Market()
    {
        // arrange
        var processor = Get<QueryProcessor>();
        var order = InitMarketOrder(_clientOrderId, _range, Symbol, OrderSide.Buy, 10.5m).ToOrder();
        var request = ModifyToMarketOrder(order, OrderSide.Buy, 11.3m);

        // act
        processor.BuildModifyOrderQuery(request).EnsureFailed();
    }

    [Fact]
    public void ModifyOrder_StopLossMarket()
    {
        // arrange
        var processor = Get<QueryProcessor>();
        var order = InitStopLossMarketOrder(_clientOrderId, _range, Symbol, OrderSide.Sell, 10.5m, 9.4m).ToOrder();
        var request = ModifyToStopLossMarketOrder(order, OrderSide.Sell, 11.3m, 12.7m);

        // act
        processor.BuildModifyOrderQuery(request).EnsureFailed();
    }

    [Fact]
    public void ModifyOrder_TakeProfitMarket()
    {
        // arrange
        var processor = Get<QueryProcessor>();
        var order = InitTakeProfitMarketOrder(_clientOrderId, _range, Symbol, OrderSide.Sell, 10.5m, 9.4m).ToOrder();
        var request = ModifyToTakeProfitMarketOrder(order, OrderSide.Sell, 11.3m, 12.7m);

        // act
        processor.BuildModifyOrderQuery(request).EnsureFailed();
    }

    [Fact]
    public void ModifyOrder_StopLossLimit()
    {
        // arrange
        var processor = Get<QueryProcessor>();
        var order = InitStopLossLimitOrder(_clientOrderId, _range, Symbol, OrderSide.Sell, 10.5m, 9.4m, 9.6m).ToOrder();
        var request = ModifyToStopLossLimitOrder(order, OrderSide.Sell, 11.3m, 12.7m, 12.9m);

        // act
        processor.BuildModifyOrderQuery(request).EnsureFailed();
    }

    [Fact]
    public void ModifyOrder_TakeProfitLimit()
    {
        // arrange
        var processor = Get<QueryProcessor>();
        var order = InitTakeProfitLimitOrder(_clientOrderId, _range, Symbol, OrderSide.Sell, 10.5m, 9.4m, 9.2m)
            .ToOrder();
        var request = ModifyToTakeProfitLimitOrder(order, OrderSide.Sell, 11.3m, 12.7m, 12.5m);

        // act
        processor.BuildModifyOrderQuery(request).EnsureFailed();
    }

    [Fact]
    public void CancelOrder()
    {
        // arrange
        var processor = Get<QueryProcessor>();
        var order = InitTakeProfitLimitOrder(_clientOrderId, _range, Symbol, OrderSide.Sell, 10.5m, 9.4m, 9.2m)
            .ToOrder();
        var request = RequestBuilder.CancelOrder(order.Id, order.ClientOrderId, order.Symbol);

        // act
        var data = processor.BuildCancelOrderQuery(request).Unwrap().As<IReadOnlyDictionary<string, string>>();

        // assert
        data.Has(3);
        data.At("origClientOrderId").Is(request.ClientOrderId);
        data.At("newClientOrderId").Is(request.ClientOrderId);
        data.At("symbol").Is(request.Symbol);
    }

    [Fact]
    public void CancelAllOrders()
    {
        // arrange
        var processor = Get<QueryProcessor>();

        // act
        var data = processor.BuildCancelAllOrdersQuery(Symbol).Unwrap().As<IReadOnlyDictionary<string, string>>();

        // assert
        data.Has(1);
        data.At("symbol").Is(Symbol);
    }
}
