using System;
using System.Collections.Generic;
using Annium.Finance.Providers.Abstractions.Connectors.Connectors;
using Annium.Finance.Providers.Abstractions.Domain.Enums;
using Annium.Finance.Providers.Tests.Shared.Connectors;
using Annium.Finance.Providers.Tests.Shared.Extensions;
using Annium.Testing;
using Xunit;
using Xunit.Abstractions;
using static Annium.Finance.Providers.Abstractions.Domain.Tools.RequestBuilder;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Tests.Internal.Connectors;

public class QueryProcessorTests : ConnectorTestBase
{
    private const string Symbol = "BTCUSDT";
    private static readonly Guid ClientOrderId = Guid.NewGuid();

    public QueryProcessorTests(ITestOutputHelper outputHelper)
        : base(ctx => ctx.WithBinanceSpot(), outputHelper) { }

    [Fact]
    public void InitOrder_Limit()
    {
        // arrange
        var processor = Get<IQueryProcessor>();
        var request = InitLimitOrder(ClientOrderId, Symbol, OrderSide.Buy, 10.5m, 15.2m);

        // act
        var data = processor.BuildInitOrderQuery(request).Unwrap().As<IReadOnlyDictionary<string, string>>();

        // assert
        data.Has(8);
        data.At("newClientOrderId").Is(request.Id.ToString());
        data.At("symbol").Is(request.Symbol);
        data.At("side").Is("BUY");
        data.At("type").Is("LIMIT");
        data.At("timeInForce").Is("GTC");
        data.At("newOrderRespType").Is("RESULT");
        data.At("quantity").Is("10.5");
        data.At("price").Is("15.2");
    }

    [Fact]
    public void InitOrder_Market()
    {
        // arrange
        var processor = Get<IQueryProcessor>();
        var request = InitMarketOrder(ClientOrderId, Symbol, OrderSide.Buy, 10.5m);

        // act
        var data = processor.BuildInitOrderQuery(request).Unwrap().As<IReadOnlyDictionary<string, string>>();

        // assert
        data.Has(6);
        data.At("newClientOrderId").Is(request.Id.ToString());
        data.At("symbol").Is(request.Symbol);
        data.At("side").Is("BUY");
        data.At("type").Is("MARKET");
        data.At("newOrderRespType").Is("RESULT");
        data.At("quantity").Is("10.5");
    }

    [Fact]
    public void InitOrder_StopLossMarket()
    {
        // arrange
        var processor = Get<IQueryProcessor>();
        var request = InitStopLossMarketOrder(ClientOrderId, Symbol, OrderSide.Sell, 10.5m, 9.4m);

        // act
        var data = processor.BuildInitOrderQuery(request).Unwrap().As<IReadOnlyDictionary<string, string>>();

        // assert
        data.Has(7);
        data.At("newClientOrderId").Is(request.Id.ToString());
        data.At("symbol").Is(request.Symbol);
        data.At("side").Is("SELL");
        data.At("type").Is("STOP_LOSS");
        data.At("newOrderRespType").Is("RESULT");
        data.At("quantity").Is("10.5");
        data.At("stopPrice").Is("9.4");
    }

    [Fact]
    public void InitOrder_TakeProfitMarket()
    {
        // arrange
        var processor = Get<IQueryProcessor>();
        var request = InitTakeProfitMarketOrder(ClientOrderId, Symbol, OrderSide.Sell, 10.5m, 9.4m);

        // act
        var data = processor.BuildInitOrderQuery(request).Unwrap().As<IReadOnlyDictionary<string, string>>();

        // assert
        data.Has(7);
        data.At("newClientOrderId").Is(request.Id.ToString());
        data.At("symbol").Is(request.Symbol);
        data.At("side").Is("SELL");
        data.At("type").Is("TAKE_PROFIT");
        data.At("newOrderRespType").Is("RESULT");
        data.At("quantity").Is("10.5");
        data.At("stopPrice").Is("9.4");
    }

    [Fact]
    public void InitOrder_StopLossLimit()
    {
        // arrange
        var processor = Get<IQueryProcessor>();
        var request = InitStopLossLimitOrder(ClientOrderId, Symbol, OrderSide.Sell, 10.5m, 9.4m, 9.6m);

        // act
        var data = processor.BuildInitOrderQuery(request).Unwrap().As<IReadOnlyDictionary<string, string>>();

        // assert
        data.Has(9);
        data.At("newClientOrderId").Is(request.Id.ToString());
        data.At("symbol").Is(request.Symbol);
        data.At("side").Is("SELL");
        data.At("type").Is("STOP_LOSS_LIMIT");
        data.At("timeInForce").Is("GTC");
        data.At("newOrderRespType").Is("RESULT");
        data.At("quantity").Is("10.5");
        data.At("price").Is("9.4");
        data.At("stopPrice").Is("9.6");
    }

    [Fact]
    public void InitOrder_TakeProfitLimit()
    {
        // arrange
        var processor = Get<IQueryProcessor>();
        var request = InitTakeProfitLimitOrder(ClientOrderId, Symbol, OrderSide.Sell, 10.5m, 9.4m, 9.2m);

        // act
        var data = processor.BuildInitOrderQuery(request).Unwrap().As<IReadOnlyDictionary<string, string>>();

        // assert
        data.Has(9);
        data.At("newClientOrderId").Is(request.Id.ToString());
        data.At("symbol").Is(request.Symbol);
        data.At("side").Is("SELL");
        data.At("type").Is("TAKE_PROFIT_LIMIT");
        data.At("timeInForce").Is("GTC");
        data.At("newOrderRespType").Is("RESULT");
        data.At("quantity").Is("10.5");
        data.At("price").Is("9.4");
        data.At("stopPrice").Is("9.2");
    }

    [Fact]
    public void ModifyOrder_Limit()
    {
        // arrange
        var processor = Get<IQueryProcessor>();
        var order = InitLimitOrder(ClientOrderId, Symbol, OrderSide.Buy, 10.5m, 15.2m).ToOrder();
        var request = ModifyToLimitOrder(order, OrderSide.Buy, 11.3m, 12.7m);

        // act
        var data = processor.BuildModifyOrderQuery(request).Unwrap().As<IReadOnlyDictionary<string, string>>();

        // assert
        data.Has(10);
        data.At("symbol").Is(request.Order.Symbol);
        data.At("side").Is("BUY");
        data.At("type").Is("LIMIT");
        data.At("cancelReplaceMode").Is("STOP_ON_FAILURE");
        data.At("timeInForce").Is("GTC");
        data.At("newOrderRespType").Is("RESULT");
        data.At("quantity").Is("11.3");
        data.At("price").Is("12.7");
        data.At("cancelOrigClientOrderId").Is(request.Order.Id.ToString());
        data.At("newClientOrderId").Is(request.Order.Id.ToString());
    }

    [Fact]
    public void ModifyOrder_Market()
    {
        // arrange
        var processor = Get<IQueryProcessor>();
        var order = InitMarketOrder(ClientOrderId, Symbol, OrderSide.Buy, 10.5m).ToOrder();
        var request = ModifyToMarketOrder(order, OrderSide.Buy, 11.3m);

        // act
        var data = processor.BuildModifyOrderQuery(request).Unwrap().As<IReadOnlyDictionary<string, string>>();

        // assert
        data.Has(9);
        data.At("symbol").Is(request.Order.Symbol);
        data.At("side").Is("BUY");
        data.At("type").Is("MARKET");
        data.At("cancelReplaceMode").Is("STOP_ON_FAILURE");
        data.At("timeInForce").Is("GTC");
        data.At("newOrderRespType").Is("RESULT");
        data.At("quantity").Is("11.3");
        data.At("cancelOrigClientOrderId").Is(request.Order.Id.ToString());
        data.At("newClientOrderId").Is(request.Order.Id.ToString());
    }

    [Fact]
    public void ModifyOrder_StopLossMarket()
    {
        // arrange
        var processor = Get<IQueryProcessor>();
        var order = InitStopLossMarketOrder(ClientOrderId, Symbol, OrderSide.Sell, 10.5m, 9.4m).ToOrder();
        var request = ModifyToStopLossMarketOrder(order, OrderSide.Sell, 11.3m, 12.7m);

        // act
        var data = processor.BuildModifyOrderQuery(request).Unwrap().As<IReadOnlyDictionary<string, string>>();

        // assert
        data.Has(10);
        data.At("symbol").Is(request.Order.Symbol);
        data.At("side").Is("SELL");
        data.At("type").Is("STOP_LOSS");
        data.At("cancelReplaceMode").Is("STOP_ON_FAILURE");
        data.At("timeInForce").Is("GTC");
        data.At("newOrderRespType").Is("RESULT");
        data.At("quantity").Is("11.3");
        data.At("stopPrice").Is("12.7");
        data.At("cancelOrigClientOrderId").Is(request.Order.Id.ToString());
        data.At("newClientOrderId").Is(request.Order.Id.ToString());
    }

    [Fact]
    public void ModifyOrder_TakeProfitMarket()
    {
        // arrange
        var processor = Get<IQueryProcessor>();
        var order = InitTakeProfitMarketOrder(ClientOrderId, Symbol, OrderSide.Sell, 10.5m, 9.4m).ToOrder();
        var request = ModifyToTakeProfitMarketOrder(order, OrderSide.Sell, 11.3m, 12.7m);

        // act
        var data = processor.BuildModifyOrderQuery(request).Unwrap().As<IReadOnlyDictionary<string, string>>();

        // assert
        data.Has(10);
        data.At("symbol").Is(request.Order.Symbol);
        data.At("side").Is("SELL");
        data.At("type").Is("TAKE_PROFIT");
        data.At("cancelReplaceMode").Is("STOP_ON_FAILURE");
        data.At("timeInForce").Is("GTC");
        data.At("newOrderRespType").Is("RESULT");
        data.At("quantity").Is("11.3");
        data.At("stopPrice").Is("12.7");
        data.At("cancelOrigClientOrderId").Is(request.Order.Id.ToString());
        data.At("newClientOrderId").Is(request.Order.Id.ToString());
    }

    [Fact]
    public void ModifyOrder_StopLossLimit()
    {
        // arrange
        var processor = Get<IQueryProcessor>();
        var order = InitStopLossLimitOrder(ClientOrderId, Symbol, OrderSide.Sell, 10.5m, 9.4m, 9.6m).ToOrder();
        var request = ModifyToStopLossLimitOrder(order, OrderSide.Sell, 11.3m, 12.7m, 12.9m);

        // act
        var data = processor.BuildModifyOrderQuery(request).Unwrap().As<IReadOnlyDictionary<string, string>>();

        // assert
        data.Has(11);
        data.At("symbol").Is(request.Order.Symbol);
        data.At("side").Is("SELL");
        data.At("type").Is("STOP_LOSS_LIMIT");
        data.At("quantity").Is("11.3");
        data.At("cancelReplaceMode").Is("STOP_ON_FAILURE");
        data.At("timeInForce").Is("GTC");
        data.At("newOrderRespType").Is("RESULT");
        data.At("price").Is("12.7");
        data.At("stopPrice").Is("12.9");
        data.At("cancelOrigClientOrderId").Is(request.Order.Id.ToString());
        data.At("newClientOrderId").Is(request.Order.Id.ToString());
    }

    [Fact]
    public void ModifyOrder_TakeProfitLimit()
    {
        // arrange
        var processor = Get<IQueryProcessor>();
        var order = InitTakeProfitLimitOrder(ClientOrderId, Symbol, OrderSide.Sell, 10.5m, 9.4m, 9.2m).ToOrder();
        var request = ModifyToTakeProfitLimitOrder(order, OrderSide.Sell, 11.3m, 12.7m, 12.5m);

        // act
        var data = processor.BuildModifyOrderQuery(request).Unwrap().As<IReadOnlyDictionary<string, string>>();

        // assert
        data.Has(11);
        data.At("symbol").Is(request.Order.Symbol);
        data.At("side").Is("SELL");
        data.At("type").Is("TAKE_PROFIT_LIMIT");
        data.At("quantity").Is("11.3");
        data.At("cancelReplaceMode").Is("STOP_ON_FAILURE");
        data.At("timeInForce").Is("GTC");
        data.At("newOrderRespType").Is("RESULT");
        data.At("price").Is("12.7");
        data.At("stopPrice").Is("12.5");
        data.At("cancelOrigClientOrderId").Is(request.Order.Id.ToString());
        data.At("newClientOrderId").Is(request.Order.Id.ToString());
    }

    [Fact]
    public void CancelOrder()
    {
        // arrange
        var processor = Get<IQueryProcessor>();
        var order = InitTakeProfitLimitOrder(ClientOrderId, Symbol, OrderSide.Sell, 10.5m, 9.4m, 9.2m).ToOrder();

        // act
        var data = processor.BuildCancelOrderQuery(order).Unwrap().As<IReadOnlyDictionary<string, string>>();

        // assert
        data.Has(3);
        data.At("symbol").Is(order.Symbol);
        data.At("origClientOrderId").Is(order.Id.ToString());
        data.At("newClientOrderId").Is(order.Id.ToString());
    }

    [Fact]
    public void CancelAllOrders()
    {
        // arrange
        var processor = Get<IQueryProcessor>();

        // act
        var data = processor.BuildCancelAllOrdersQuery(Symbol).Unwrap().As<IReadOnlyDictionary<string, string>>();

        // assert
        data.Has(1);
        data.At("symbol").Is(Symbol);
    }
}
