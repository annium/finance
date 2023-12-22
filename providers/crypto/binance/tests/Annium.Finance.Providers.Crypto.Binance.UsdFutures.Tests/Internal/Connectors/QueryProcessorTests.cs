using System;
using System.Collections.Generic;
using System.Globalization;
using Annium.Finance.Providers.Abstractions.Connectors.Connectors;
using Annium.Finance.Providers.Abstractions.Domain.Enums;
using Annium.Finance.Providers.Tests.Shared.Connectors;
using Annium.Finance.Providers.Tests.Shared.Extensions;
using Annium.Testing;
using Xunit;
using Xunit.Abstractions;
using static Annium.Finance.Providers.Abstractions.Domain.Tools.RequestBuilder;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Tests.Internal.Connectors;

public class QueryProcessorTests : ConnectorTestBase
{
    private const string Symbol = "BTCUSDT";
    private static readonly Guid ClientOrderId = Guid.NewGuid();

    public QueryProcessorTests(ITestOutputHelper outputHelper)
        : base(ctx => ctx.WithBinanceUsdFutures(), outputHelper) { }

    [Fact]
    public void InitOrder_Limit()
    {
        // arrange
        var processor = Get<IQueryProcessor>();
        var request = InitLimitOrder(ClientOrderId, Symbol, OrderSide.Buy, 10.5m, 15.2m);

        // act
        var data = processor.BuildInitOrderQuery(request).Unwrap().As<IReadOnlyDictionary<string, string>>();

        // assert
        data.Has(10);
        data.At("newClientOrderId").Is(request.Id.ToString());
        data.At("symbol").Is(request.Symbol);
        data.At("side").Is("BUY");
        data.At("positionSide").Is("BOTH");
        data.At("type").Is("LIMIT");
        data.At("timeInForce").Is("GTC");
        data.At("newOrderRespType").Is("RESULT");
        data.At("quantity").Is("10.5");
        data.At("price").Is("15.2");
    }

    [Fact]
    public void InitOrder_Limit_ReduceOnly()
    {
        // arrange
        var processor = Get<IQueryProcessor>();
        var request = InitLimitOrder(ClientOrderId, Symbol, OrderSide.Buy, 10.5m, 15.2m);

        // act
        var data = processor.BuildInitOrderQuery(request).Unwrap().As<IReadOnlyDictionary<string, string>>();

        // assert
        data.Has(10);
        data.At("newClientOrderId").Is(request.Id.ToString());
        data.At("symbol").Is(request.Symbol);
        data.At("side").Is("BUY");
        data.At("positionSide").Is("BOTH");
        data.At("type").Is("LIMIT");
        data.At("timeInForce").Is("GTC");
        data.At("newOrderRespType").Is("RESULT");
        data.At("quantity").Is("10.5");
        data.At("price").Is("15.2");
        data.At("reduceOnly").Is("true");
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
        data.Has(8);
        data.At("newClientOrderId").Is(request.Id.ToString());
        data.At("symbol").Is(request.Symbol);
        data.At("side").Is("BUY");
        data.At("positionSide").Is("BOTH");
        data.At("type").Is("MARKET");
        data.At("newOrderRespType").Is("RESULT");
        data.At("quantity").Is("10.5");
    }

    [Fact]
    public void InitOrder_Market_ReduceOnly()
    {
        // arrange
        var processor = Get<IQueryProcessor>();
        var request = InitMarketOrder(ClientOrderId, Symbol, OrderSide.Buy, 10.5m);

        // act
        var data = processor.BuildInitOrderQuery(request).Unwrap().As<IReadOnlyDictionary<string, string>>();

        // assert
        data.Has(8);
        data.At("newClientOrderId").Is(request.Id.ToString());
        data.At("symbol").Is(request.Symbol);
        data.At("side").Is("BUY");
        data.At("positionSide").Is("BOTH");
        data.At("type").Is("MARKET");
        data.At("newOrderRespType").Is("RESULT");
        data.At("quantity").Is("10.5");
        data.At("reduceOnly").Is("true");
    }

    [Fact]
    public void InitOrder_StopLossMarket_Normal()
    {
        // arrange
        var processor = Get<IQueryProcessor>();
        var request = InitStopLossMarketOrder(ClientOrderId, Symbol, OrderSide.Sell, 10.5m, 9.4m);

        // act
        var data = processor.BuildInitOrderQuery(request).Unwrap().As<IReadOnlyDictionary<string, string>>();

        // assert
        data.Has(9);
        data.At("newClientOrderId").Is(request.Id.ToString());
        data.At("symbol").Is(request.Symbol);
        data.At("side").Is("SELL");
        data.At("positionSide").Is("BOTH");
        data.At("type").Is("STOP_MARKET");
        data.At("newOrderRespType").Is("RESULT");
        data.At("quantity").Is("10.5");
        data.At("stopPrice").Is("9.4");
    }

    [Theory]
    [InlineData(8, 0)]
    [InlineData(9, 10.5)]
    public void InitOrder_StopLossMarket_Normal_ReduceOnly(int paramsCount, decimal quantity)
    {
        // arrange
        var processor = Get<IQueryProcessor>();
        var request = InitStopLossMarketOrder(ClientOrderId, Symbol, OrderSide.Sell, quantity, 9.4m);

        // act
        var data = processor.BuildInitOrderQuery(request).Unwrap().As<IReadOnlyDictionary<string, string>>();

        // assert
        data.Has(paramsCount);
        data.At("newClientOrderId").Is(request.Id.ToString());
        data.At("symbol").Is(request.Symbol);
        data.At("side").Is("SELL");
        data.At("positionSide").Is("BOTH");
        data.At("type").Is("STOP_MARKET");
        data.At("newOrderRespType").Is("RESULT");
        data.At("stopPrice").Is("9.4");

        if (quantity == 0)
        {
            data.At("closePosition").Is("true");
        }
        else
        {
            data.At("quantity").Is(quantity.ToString(CultureInfo.InvariantCulture));
            data.At("reduceOnly").Is("true");
        }
    }

    [Theory]
    [InlineData(8, 0)]
    [InlineData(9, 10.5)]
    public void InitOrder_TakeProfitMarket_ReduceOnly(int paramsCount, decimal quantity)
    {
        // arrange
        var processor = Get<IQueryProcessor>();
        var request = InitTakeProfitMarketOrder(ClientOrderId, Symbol, OrderSide.Sell, quantity, 9.4m);

        // act
        var data = processor.BuildInitOrderQuery(request).Unwrap().As<IReadOnlyDictionary<string, string>>();

        // assert
        data.Has(paramsCount);
        data.At("newClientOrderId").Is(request.Id.ToString());
        data.At("symbol").Is(request.Symbol);
        data.At("side").Is("SELL");
        data.At("positionSide").Is("BOTH");
        data.At("type").Is("TAKE_PROFIT_MARKET");
        data.At("newOrderRespType").Is("RESULT");
        data.At("stopPrice").Is("9.4");

        if (quantity == 0)
        {
            data.At("closePosition").Is("true");
        }
        else
        {
            data.At("quantity").Is(quantity.ToString(CultureInfo.InvariantCulture));
            data.At("reduceOnly").Is("true");
        }
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
        data.Has(11);
        data.At("newClientOrderId").Is(request.Id.ToString());
        data.At("symbol").Is(request.Symbol);
        data.At("side").Is("SELL");
        data.At("positionSide").Is("BOTH");
        data.At("type").Is("STOP");
        data.At("timeInForce").Is("GTC");
        data.At("newOrderRespType").Is("RESULT");
        data.At("quantity").Is("10.5");
        data.At("price").Is("9.4");
        data.At("stopPrice").Is("9.6");
        data.At("reduceOnly").Is("true");
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
        data.Has(11);
        data.At("newClientOrderId").Is(request.Id.ToString());
        data.At("symbol").Is(request.Symbol);
        data.At("side").Is("SELL");
        data.At("positionSide").Is("BOTH");
        data.At("type").Is("TAKE_PROFIT");
        data.At("timeInForce").Is("GTC");
        data.At("newOrderRespType").Is("RESULT");
        data.At("quantity").Is("10.5");
        data.At("price").Is("9.4");
        data.At("stopPrice").Is("9.2");
        data.At("reduceOnly").Is("true");
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
        data.Has(5);
        data.At("symbol").Is(request.Order.Symbol);
        data.At("side").Is("BUY");
        data.At("quantity").Is("11.3");
        data.At("price").Is("12.7");
        data.At("origClientOrderId").Is(request.Order.ClientOrderId.ToString());
    }

    [Fact]
    public void ModifyOrder_Market()
    {
        // arrange
        var processor = Get<IQueryProcessor>();
        var order = InitMarketOrder(ClientOrderId, Symbol, OrderSide.Buy, 10.5m).ToOrder();
        var request = ModifyToMarketOrder(order, OrderSide.Buy, 11.3m);

        // act
        processor.BuildModifyOrderQuery(request).EnsureFailed();
    }

    [Fact]
    public void ModifyOrder_StopLossMarket()
    {
        // arrange
        var processor = Get<IQueryProcessor>();
        var order = InitStopLossMarketOrder(ClientOrderId, Symbol, OrderSide.Sell, 10.5m, 9.4m).ToOrder();
        var request = ModifyToStopLossMarketOrder(order, OrderSide.Sell, 11.3m, 12.7m);

        // act
        processor.BuildModifyOrderQuery(request).EnsureFailed();
    }

    [Fact]
    public void ModifyOrder_TakeProfitMarket()
    {
        // arrange
        var processor = Get<IQueryProcessor>();
        var order = InitTakeProfitMarketOrder(ClientOrderId, Symbol, OrderSide.Sell, 10.5m, 9.4m).ToOrder();
        var request = ModifyToTakeProfitMarketOrder(order, OrderSide.Sell, 11.3m, 12.7m);

        // act
        processor.BuildModifyOrderQuery(request).EnsureFailed();
    }

    [Fact]
    public void ModifyOrder_StopLossLimit()
    {
        // arrange
        var processor = Get<IQueryProcessor>();
        var order = InitStopLossLimitOrder(ClientOrderId, Symbol, OrderSide.Sell, 10.5m, 9.4m, 9.6m).ToOrder();
        var request = ModifyToStopLossLimitOrder(order, OrderSide.Sell, 11.3m, 12.7m, 12.9m);

        // act
        processor.BuildModifyOrderQuery(request).EnsureFailed();
    }

    [Fact]
    public void ModifyOrder_TakeProfitLimit()
    {
        // arrange
        var processor = Get<IQueryProcessor>();
        var order = InitTakeProfitLimitOrder(ClientOrderId, Symbol, OrderSide.Sell, 10.5m, 9.4m, 9.2m).ToOrder();
        var request = ModifyToTakeProfitLimitOrder(order, OrderSide.Sell, 11.3m, 12.7m, 12.5m);

        // act
        processor.BuildModifyOrderQuery(request).EnsureFailed();
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
        data.At("origClientOrderId").Is(order.ClientOrderId.ToString());
        data.At("newClientOrderId").Is(order.ClientOrderId.ToString());
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
