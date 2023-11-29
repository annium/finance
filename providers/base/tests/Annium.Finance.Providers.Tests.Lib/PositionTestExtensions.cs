using System;
using Annium.Finance.Providers.Abstractions.Domain.Enums;
using Annium.Finance.Providers.Abstractions.Domain.Extensions;
using Annium.Finance.Providers.Tests.Lib.Models;

namespace Annium.Finance.Providers.Tests.Lib;

public static class PositionTestExtensions
{
    #region new limit order

    public static Order NewLimitBuyOrder(this Position position, decimal totalQty, decimal price) =>
        position.NewLimitOrder(OrderSide.Buy, totalQty, price);

    public static Order NewLimitSellOrder(this Position position, decimal totalQty, decimal price) =>
        position.NewLimitOrder(OrderSide.Sell, totalQty, price);

    public static Order NewLimitOrder(this Position position, OrderSide side, decimal totalQty, decimal price) =>
        new(Guid.NewGuid(), position, side, OrderType.Limit, totalQty, price, 0, 0, OrderStatus.New, 0, 0, 0, 0);

    #endregion

    #region new market order

    public static Order NewMarketBuyOrder(this Position position, decimal totalQty) =>
        position.NewMarketOrder(OrderSide.Buy, totalQty);

    public static Order NewMarketSellOrder(this Position position, decimal totalQty) =>
        position.NewMarketOrder(OrderSide.Sell, totalQty);

    public static Order NewMarketOrder(this Position position, OrderSide side, decimal totalQty) =>
        new(Guid.NewGuid(), position, side, OrderType.Market, totalQty, 0, 0, 0, OrderStatus.New, 0, 0, 0, 0);

    #endregion

    #region new take profit market order

    public static Order NewTakeProfitMarketBuyOrder(this Position position, decimal totalQty, decimal levelPrice) =>
        position.NewTakeProfitMarketOrder(OrderSide.Buy, totalQty, levelPrice);

    public static Order NewTakeProfitMarketSellOrder(this Position position, decimal totalQty, decimal levelPrice) =>
        position.NewTakeProfitMarketOrder(OrderSide.Sell, totalQty, levelPrice);

    public static Order NewTakeProfitMarketOrder(
        this Position position,
        OrderSide side,
        decimal totalQty,
        decimal levelPrice
    ) =>
        new(
            Guid.NewGuid(),
            position,
            side,
            OrderType.TakeProfitMarket,
            totalQty,
            0,
            levelPrice,
            0,
            OrderStatus.New,
            0,
            0,
            0,
            0
        );

    #endregion

    #region new stop loss market order

    public static Order NewStopLossMarketBuyOrder(this Position position, decimal totalQty, decimal levelPrice) =>
        position.NewStopLossMarketOrder(OrderSide.Buy, totalQty, levelPrice);

    public static Order NewStopLossMarketSellOrder(this Position position, decimal totalQty, decimal levelPrice) =>
        position.NewStopLossMarketOrder(OrderSide.Sell, totalQty, levelPrice);

    public static Order NewStopLossMarketOrder(
        this Position position,
        OrderSide side,
        decimal totalQty,
        decimal levelPrice
    ) =>
        new(
            Guid.NewGuid(),
            position,
            side,
            OrderType.StopLossMarket,
            totalQty,
            0,
            levelPrice,
            0,
            OrderStatus.New,
            0,
            0,
            0,
            0
        );

    #endregion

    #region add limit order

    public static Order AddLimitBuyOrder(this Position position, decimal totalQty, decimal price) =>
        position.NewLimitBuyOrder(totalQty, price).AddToPosition();

    public static Order AddLimitSellOrder(this Position position, decimal totalQty, decimal price) =>
        position.NewLimitSellOrder(totalQty, price).AddToPosition();

    public static Order AddLimitOrder(this Position position, OrderSide side, decimal totalQty, decimal price) =>
        position.NewLimitOrder(side, totalQty, price).AddToPosition();

    #endregion

    #region add market order

    public static Order AddMarketBuyOrder(this Position position, decimal totalQty) =>
        position.NewMarketBuyOrder(totalQty).AddToPosition();

    public static Order AddMarketSellOrder(this Position position, decimal totalQty) =>
        position.NewMarketSellOrder(totalQty).AddToPosition();

    public static Order AddMarketOrder(this Position position, OrderSide side, decimal totalQty) =>
        position.NewMarketOrder(side, totalQty).AddToPosition();

    #endregion

    #region add take profit market order

    public static Order AddTakeProfitMarketBuyOrder(this Position position, decimal totalQty, decimal price) =>
        position.NewTakeProfitMarketBuyOrder(totalQty, price).AddToPosition();

    public static Order AddTakeProfitMarketSellOrder(this Position position, decimal totalQty, decimal price) =>
        position.NewTakeProfitMarketSellOrder(totalQty, price).AddToPosition();

    public static Order AddTakeProfitMarketOrder(
        this Position position,
        OrderSide side,
        decimal totalQty,
        decimal price
    ) => position.NewTakeProfitMarketOrder(side, totalQty, price).AddToPosition();

    #endregion

    #region add stop loss market order

    public static Order AddStopLossMarketBuyOrder(this Position position, decimal totalQty, decimal price) =>
        position.NewStopLossMarketBuyOrder(totalQty, price).AddToPosition();

    public static Order AddStopLossMarketSellOrder(this Position position, decimal totalQty, decimal price) =>
        position.NewStopLossMarketSellOrder(totalQty, price).AddToPosition();

    public static Order AddStopLossMarketOrder(
        this Position position,
        OrderSide side,
        decimal totalQty,
        decimal price
    ) => position.NewStopLossMarketOrder(side, totalQty, price).AddToPosition();

    #endregion

    #region helpers

    public static Position RemoveOrder(this Position position, Order order) =>
        position.RemoveOrder(
            order.Id,
            order.Side,
            order.TotalQty,
            order.PotentialQty(),
            order.ExecutedQty,
            order.ExecutedPrice,
            order.Fee,
            order.UpdatedAt
        );

    #endregion
}
