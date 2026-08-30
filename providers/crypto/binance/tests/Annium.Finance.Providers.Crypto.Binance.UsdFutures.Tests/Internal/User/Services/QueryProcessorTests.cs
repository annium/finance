using System;
using System.Collections.Generic;
using System.Globalization;
using Annium.Finance.Providers.Abstractions.Domain.User;
using Annium.Finance.Providers.Abstractions.Domain.User.Requests;
using Annium.Finance.Providers.Core;
using Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.User.Services;
using Annium.Finance.Providers.Tests.Lib;
using Annium.Finance.Providers.Tests.Lib.User.Operations;
using Annium.Finance.Providers.Tests.Lib.User.Requests;
using Annium.Testing;
using Xunit;
using static Annium.Finance.Providers.Abstractions.Domain.User.Requests.RequestBuilder;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Tests.Internal.User.Services;

/// <summary>
/// Verifies that <c>QueryProcessor</c> turns each typed order request into the exact query-parameter set
/// Binance's USD-M futures REST API expects: the right field names, casing and order-type names (which
/// differ from Spot's, e.g. <c>STOP</c>/<c>STOP_MARKET</c> instead of <c>STOP_LOSS(_LIMIT)</c>) for every
/// order type, the optional <c>reduceOnly</c> flag only appearing when set, and that a plain
/// <c>positionSide=BOTH</c> is always included; also that modify requests only support changing a
/// <c>LIMIT</c> order (any other type fails to build a query), and that cancel requests carry just the
/// identifying fields.
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
    /// Registers the Binance USD-M futures provider so the query processor under test is resolved from its actual registration.
    /// </summary>
    /// <param name="ctx">The fluent context to register providers into.</param>
    protected override void RegisterProvider(ProviderRegistrationContext ctx)
    {
        ctx.WithBinanceUsdFutures();
    }

    /// <summary>
    /// A limit order request builds a query with symbol, side, <c>positionSide=BOTH</c>, <c>LIMIT</c> type,
    /// GTC time-in-force, quantity and price, plus <c>reduceOnly</c> only when the request sets it.
    /// </summary>
    /// <param name="count">The expected number of query fields, which grows by one when <paramref name="reduceOnly"/> is set.</param>
    /// <param name="reduceOnly">Whether the request marks the order reduce-only.</param>
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

    /// <summary>
    /// A market order request builds a query with symbol, side, <c>positionSide=BOTH</c>, <c>MARKET</c>
    /// type and quantity (no time-in-force or price), plus <c>reduceOnly</c> only when the request sets it.
    /// </summary>
    /// <param name="count">The expected number of query fields, which grows by one when <paramref name="reduceOnly"/> is set.</param>
    /// <param name="reduceOnly">Whether the request marks the order reduce-only.</param>
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

    /// <summary>
    /// A stop-loss-market order request builds a query with the futures-specific <c>STOP_MARKET</c> type
    /// and the trigger under <c>stopPrice</c>, plus <c>reduceOnly</c> only when the request sets it.
    /// </summary>
    /// <param name="count">The expected number of query fields, which grows by one when <paramref name="reduceOnly"/> is set.</param>
    /// <param name="quantity">The order quantity.</param>
    /// <param name="reduceOnly">Whether the request marks the order reduce-only.</param>
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

    /// <summary>
    /// A take-profit-market order request builds a query with the <c>TAKE_PROFIT_MARKET</c> type and the
    /// trigger under <c>stopPrice</c>, plus <c>reduceOnly</c> only when the request sets it.
    /// </summary>
    /// <param name="count">The expected number of query fields, which grows by one when <paramref name="reduceOnly"/> is set.</param>
    /// <param name="quantity">The order quantity.</param>
    /// <param name="reduceOnly">Whether the request marks the order reduce-only.</param>
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

    /// <summary>
    /// A stop-loss-limit order request builds a query with the futures-specific <c>STOP</c> type, GTC
    /// time-in-force, and both a limit <c>price</c> and a trigger <c>stopPrice</c>, plus <c>reduceOnly</c>
    /// only when the request sets it.
    /// </summary>
    /// <param name="count">The expected number of query fields, which grows by one when <paramref name="reduceOnly"/> is set.</param>
    /// <param name="reduceOnly">Whether the request marks the order reduce-only.</param>
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

    /// <summary>
    /// A take-profit-limit order request builds a query with the <c>TAKE_PROFIT</c> type, GTC time-in-force,
    /// and both a limit <c>price</c> and a trigger <c>stopPrice</c>, plus <c>reduceOnly</c> only when the
    /// request sets it.
    /// </summary>
    /// <param name="count">The expected number of query fields, which grows by one when <paramref name="reduceOnly"/> is set.</param>
    /// <param name="reduceOnly">Whether the request marks the order reduce-only.</param>
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

    /// <summary>
    /// Modifying a limit order builds a plain "amend" query (no cancel-replace mechanics, unlike Spot) with
    /// the new symbol, side, quantity, price and the original client order id.
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
        data.Has(5);
        data.At("symbol").Is(request.Order.Symbol);
        data.At("side").Is("BUY");
        data.At("quantity").Is("11.3");
        data.At("price").Is("12.7");
        data.At("origClientOrderId").Is(request.Order.ClientOrderId);
    }

    /// <summary>
    /// Binance's futures amend-order endpoint only supports limit orders, so a modify-to-market request
    /// fails to build a query rather than producing one.
    /// </summary>
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

    /// <summary>
    /// Binance's futures amend-order endpoint only supports limit orders, so a modify-to-stop-loss-market
    /// request fails to build a query rather than producing one.
    /// </summary>
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

    /// <summary>
    /// Binance's futures amend-order endpoint only supports limit orders, so a modify-to-take-profit-market
    /// request fails to build a query rather than producing one.
    /// </summary>
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

    /// <summary>
    /// Binance's futures amend-order endpoint only supports limit orders, so a modify-to-stop-loss-limit
    /// request fails to build a query rather than producing one.
    /// </summary>
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

    /// <summary>
    /// Binance's futures amend-order endpoint only supports limit orders, so a modify-to-take-profit-limit
    /// request fails to build a query rather than producing one.
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
        processor.BuildModifyOrderQuery(request).EnsureFailed();
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
