using System;
using System.Collections.Generic;
using Annium.Finance.Providers.Abstractions.Domain.Enums;
using Annium.Finance.Providers.Abstractions.Domain.Tools;
using Annium.Finance.Providers.Crypto.Binance.Spot.Internal.Connectors;
using Annium.Finance.Providers.Shared;
using Annium.Finance.Providers.Tests.Lib;
using Annium.Finance.Providers.Tests.Lib.Extensions;
using Annium.Testing;
using Xunit;
using static Annium.Finance.Providers.Abstractions.Domain.Tools.RequestBuilder;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Tests.Internal.Connectors;

public class QueryProcessorTests : ProvidersTestBase
{
    private const string Symbol = "BTCUSDT";
    private static readonly string _clientOrderId = Guid.NewGuid().ToString();
    private static readonly OrientationRange _range = OrientationRange.Both;

    public QueryProcessorTests(ITestOutputHelper outputHelper)
        : base(outputHelper) { }

    protected override void RegisterProvider(ProviderRegistrationContext ctx)
    {
        ctx.WithBinanceSpot();
    }

    [Fact]
    public void InitOrder_Limit()
    {
        // arrange
        var processor = Get<QueryProcessor>();
        var request = InitLimitOrder(_clientOrderId, _range, Symbol, OrderSide.Buy, 10.5m, 15.2m);

        // act
        var data = processor.BuildInitOrderQuery(request).Unwrap().As<IReadOnlyDictionary<string, string>>();

        // assert
        data.Has(8);
        data.At("newClientOrderId").Is(request.Id);
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
        var processor = Get<QueryProcessor>();
        var request = InitMarketOrder(_clientOrderId, _range, Symbol, OrderSide.Buy, 10.5m);

        // act
        var data = processor.BuildInitOrderQuery(request).Unwrap().As<IReadOnlyDictionary<string, string>>();

        // assert
        data.Has(6);
        data.At("newClientOrderId").Is(request.Id);
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
        var processor = Get<QueryProcessor>();
        var request = InitStopLossMarketOrder(_clientOrderId, _range, Symbol, OrderSide.Sell, 10.5m, 9.4m);

        // act
        var data = processor.BuildInitOrderQuery(request).Unwrap().As<IReadOnlyDictionary<string, string>>();

        // assert
        data.Has(7);
        data.At("newClientOrderId").Is(request.Id);
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
        var processor = Get<QueryProcessor>();
        var request = InitTakeProfitMarketOrder(_clientOrderId, _range, Symbol, OrderSide.Sell, 10.5m, 9.4m);

        // act
        var data = processor.BuildInitOrderQuery(request).Unwrap().As<IReadOnlyDictionary<string, string>>();

        // assert
        data.Has(7);
        data.At("newClientOrderId").Is(request.Id);
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
        var processor = Get<QueryProcessor>();
        var request = InitStopLossLimitOrder(_clientOrderId, _range, Symbol, OrderSide.Sell, 10.5m, 9.4m, 9.6m);

        // act
        var data = processor.BuildInitOrderQuery(request).Unwrap().As<IReadOnlyDictionary<string, string>>();

        // assert
        data.Has(9);
        data.At("newClientOrderId").Is(request.Id);
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
        var processor = Get<QueryProcessor>();
        var request = InitTakeProfitLimitOrder(_clientOrderId, _range, Symbol, OrderSide.Sell, 10.5m, 9.4m, 9.2m);

        // act
        var data = processor.BuildInitOrderQuery(request).Unwrap().As<IReadOnlyDictionary<string, string>>();

        // assert
        data.Has(9);
        data.At("newClientOrderId").Is(request.Id);
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
        var processor = Get<QueryProcessor>();
        var order = InitLimitOrder(_clientOrderId, _range, Symbol, OrderSide.Buy, 10.5m, 15.2m).ToOrder();
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
        data.At("cancelOrigClientOrderId").Is(request.Order.ClientOrderId);
        data.At("newClientOrderId").Is(request.Order.ClientOrderId);
    }

    [Fact]
    public void ModifyOrder_Market()
    {
        // arrange
        var processor = Get<QueryProcessor>();
        var order = InitMarketOrder(_clientOrderId, _range, Symbol, OrderSide.Buy, 10.5m).ToOrder();
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
        data.At("cancelOrigClientOrderId").Is(request.Order.ClientOrderId);
        data.At("newClientOrderId").Is(request.Order.ClientOrderId);
    }

    [Fact]
    public void ModifyOrder_StopLossMarket()
    {
        // arrange
        var processor = Get<QueryProcessor>();
        var order = InitStopLossMarketOrder(_clientOrderId, _range, Symbol, OrderSide.Sell, 10.5m, 9.4m).ToOrder();
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
        data.At("cancelOrigClientOrderId").Is(request.Order.ClientOrderId);
        data.At("newClientOrderId").Is(request.Order.ClientOrderId);
    }

    [Fact]
    public void ModifyOrder_TakeProfitMarket()
    {
        // arrange
        var processor = Get<QueryProcessor>();
        var order = InitTakeProfitMarketOrder(_clientOrderId, _range, Symbol, OrderSide.Sell, 10.5m, 9.4m).ToOrder();
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
        data.At("cancelOrigClientOrderId").Is(request.Order.ClientOrderId);
        data.At("newClientOrderId").Is(request.Order.ClientOrderId);
    }

    [Fact]
    public void ModifyOrder_StopLossLimit()
    {
        // arrange
        var processor = Get<QueryProcessor>();
        var order = InitStopLossLimitOrder(_clientOrderId, _range, Symbol, OrderSide.Sell, 10.5m, 9.4m, 9.6m).ToOrder();
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
        data.At("cancelOrigClientOrderId").Is(request.Order.ClientOrderId);
        data.At("newClientOrderId").Is(request.Order.ClientOrderId);
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
        data.At("cancelOrigClientOrderId").Is(request.Order.ClientOrderId);
        data.At("newClientOrderId").Is(request.Order.ClientOrderId);
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
