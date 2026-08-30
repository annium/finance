using System;
using System.Collections.Generic;
using Annium.Finance.Providers.Abstractions.Domain.User;
using Annium.Finance.Providers.Abstractions.Domain.User.Requests;
using Annium.Finance.Providers.Core;
using Annium.Finance.Providers.Crypto.Binance.Spot.Internal.User.Services;
using Annium.Finance.Providers.Tests.Lib;
using Annium.Finance.Providers.Tests.Lib.User.Operations;
using Annium.Finance.Providers.Tests.Lib.User.Requests;
using Annium.Testing;
using Xunit;
using static Annium.Finance.Providers.Abstractions.Domain.User.Requests.RequestBuilder;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Tests.Internal.User.Services;

/// <summary>
/// Verifies that <c>QueryProcessor</c> turns each typed order request into the exact query-parameter set
/// Binance's Spot REST API expects: the right field names and casing for every order type (limit, market,
/// stop-loss/take-profit, market and limit variants), that init requests set <c>newOrderRespType=RESULT</c>,
/// that modify requests build a cancel-replace query with <c>cancelReplaceMode=STOP_ON_FAILURE</c> and both
/// the old and new client order ids, and that cancel requests carry just the identifying fields.
/// </summary>
public class QueryProcessorTests : ProvidersTestBase
{
    /// <summary>The symbol every request in this fixture is built for.</summary>
    private const string Symbol = "BTCUSDT";

    /// <summary>The client order id shared by every request in this fixture.</summary>
    private static readonly string _clientOrderId = Guid.NewGuid().ToString();

    /// <summary>The orientation range (both/long/short) every request in this fixture is built with.</summary>
    private static readonly OrientationRange _range = OrientationRange.Both;

    /// <summary>
    /// Initializes a new instance of the <see cref="QueryProcessorTests"/> class.
    /// </summary>
    /// <param name="outputHelper">The xUnit output helper to route trace logging to.</param>
    public QueryProcessorTests(ITestOutputHelper outputHelper)
        : base(outputHelper) { }

    /// <summary>
    /// Registers the Binance Spot provider so the query processor under test is resolved from its actual registration.
    /// </summary>
    /// <param name="ctx">The fluent context to register providers into.</param>
    protected override void RegisterProvider(ProviderRegistrationContext ctx)
    {
        ctx.WithBinanceSpot();
    }

    /// <summary>
    /// A limit order request builds a query with symbol, side, <c>LIMIT</c> type, GTC time-in-force,
    /// quantity and price.
    /// </summary>
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

    /// <summary>
    /// A market order request builds a query with symbol, side, <c>MARKET</c> type and quantity, and no
    /// time-in-force or price.
    /// </summary>
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

    /// <summary>
    /// A stop-loss-market order request builds a query with the <c>STOP_LOSS</c> type and the trigger under
    /// <c>stopPrice</c>, with no time-in-force since it's a market order.
    /// </summary>
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

    /// <summary>
    /// A take-profit-market order request builds a query with the <c>TAKE_PROFIT</c> type and the trigger
    /// under <c>stopPrice</c>, with no time-in-force since it's a market order.
    /// </summary>
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

    /// <summary>
    /// A stop-loss-limit order request builds a query with the <c>STOP_LOSS_LIMIT</c> type, GTC
    /// time-in-force, and both a limit <c>price</c> and a trigger <c>stopPrice</c>.
    /// </summary>
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

    /// <summary>
    /// A take-profit-limit order request builds a query with the <c>TAKE_PROFIT_LIMIT</c> type, GTC
    /// time-in-force, and both a limit <c>price</c> and a trigger <c>stopPrice</c>.
    /// </summary>
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

    /// <summary>
    /// Modifying a limit order builds a cancel-replace query for the new <c>LIMIT</c> parameters, with
    /// <c>cancelReplaceMode=STOP_ON_FAILURE</c> and both <c>cancelOrigClientOrderId</c> and
    /// <c>newClientOrderId</c> set to the original order's client id.
    /// </summary>
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

    /// <summary>
    /// Modifying to a market order builds a cancel-replace query for the new <c>MARKET</c> parameters
    /// (quantity only, no price), still through the same cancel-replace/client-id mechanics as the other
    /// modify cases.
    /// </summary>
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

    /// <summary>
    /// Modifying to a stop-loss-market order builds a cancel-replace query for the new <c>STOP_LOSS</c>
    /// parameters, with the new trigger under <c>stopPrice</c>.
    /// </summary>
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

    /// <summary>
    /// Modifying to a take-profit-market order builds a cancel-replace query for the new <c>TAKE_PROFIT</c>
    /// parameters, with the new trigger under <c>stopPrice</c>.
    /// </summary>
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

    /// <summary>
    /// Modifying to a stop-loss-limit order builds a cancel-replace query for the new
    /// <c>STOP_LOSS_LIMIT</c> parameters, with both a new limit <c>price</c> and a new trigger
    /// <c>stopPrice</c>.
    /// </summary>
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

    /// <summary>
    /// Modifying to a take-profit-limit order builds a cancel-replace query for the new
    /// <c>TAKE_PROFIT_LIMIT</c> parameters, with both a new limit <c>price</c> and a new trigger
    /// <c>stopPrice</c>.
    /// </summary>
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

    /// <summary>
    /// A cancel request builds a minimal query carrying just the symbol and the order's client id, sent
    /// under both <c>origClientOrderId</c> (identifying the order to cancel) and <c>newClientOrderId</c>
    /// (for the cancel confirmation).
    /// </summary>
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

    /// <summary>
    /// A cancel-all-orders request builds a query carrying just the symbol.
    /// </summary>
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
