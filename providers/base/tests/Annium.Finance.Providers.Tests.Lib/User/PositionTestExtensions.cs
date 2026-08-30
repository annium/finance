using System;
using Annium.Data.Operations;
using Annium.Finance.Providers.Abstractions.Domain.User;
using Annium.Finance.Providers.Tests.Lib.Shared.Operations;

namespace Annium.Finance.Providers.Tests.Lib.User;

/// <summary>
/// Builds fake <see cref="Order"/> instances for a <see cref="Position"/> - "New*" methods just construct the
/// order, "Add*" methods also register it with the position via <see cref="OrderTestExtensions.AddToPosition"/> -
/// one pair per order type/side combination, plus a helper to drop a canceled/filled order from a position.
/// </summary>
public static class PositionTestExtensions
{
    #region new limit order

    /// <summary>Builds a new buy limit order for the position.</summary>
    /// <param name="position">The position to place the order against.</param>
    /// <param name="totalQty">The total quantity the order is placed for.</param>
    /// <param name="price">The order's limit price.</param>
    /// <returns>A new, unregistered limit order.</returns>
    public static Order NewLimitBuyOrder(this Position position, decimal totalQty, decimal price) =>
        position.NewLimitOrder(OrderSide.Buy, totalQty, price);

    /// <summary>Builds a new sell limit order for the position.</summary>
    /// <param name="position">The position to place the order against.</param>
    /// <param name="totalQty">The total quantity the order is placed for.</param>
    /// <param name="price">The order's limit price.</param>
    /// <returns>A new, unregistered limit order.</returns>
    public static Order NewLimitSellOrder(this Position position, decimal totalQty, decimal price) =>
        position.NewLimitOrder(OrderSide.Sell, totalQty, price);

    /// <summary>Builds a new limit order for the position on the given side.</summary>
    /// <param name="position">The position to place the order against.</param>
    /// <param name="side">The side (buy or sell) to place the order on.</param>
    /// <param name="totalQty">The total quantity the order is placed for.</param>
    /// <param name="price">The order's limit price.</param>
    /// <returns>A new, unregistered limit order.</returns>
    public static Order NewLimitOrder(this Position position, OrderSide side, decimal totalQty, decimal price) =>
        new(Guid.NewGuid(), position, side, OrderType.Limit, totalQty, price, 0, 0, OrderStatus.New, 0, 0, 0, 0);

    #endregion

    #region new market order

    /// <summary>Builds a new buy market order for the position.</summary>
    /// <param name="position">The position to place the order against.</param>
    /// <param name="totalQty">The total quantity the order is placed for.</param>
    /// <returns>A new, unregistered market order.</returns>
    public static Order NewMarketBuyOrder(this Position position, decimal totalQty) =>
        position.NewMarketOrder(OrderSide.Buy, totalQty);

    /// <summary>Builds a new sell market order for the position.</summary>
    /// <param name="position">The position to place the order against.</param>
    /// <param name="totalQty">The total quantity the order is placed for.</param>
    /// <returns>A new, unregistered market order.</returns>
    public static Order NewMarketSellOrder(this Position position, decimal totalQty) =>
        position.NewMarketOrder(OrderSide.Sell, totalQty);

    /// <summary>Builds a new market order for the position on the given side.</summary>
    /// <param name="position">The position to place the order against.</param>
    /// <param name="side">The side (buy or sell) to place the order on.</param>
    /// <param name="totalQty">The total quantity the order is placed for.</param>
    /// <returns>A new, unregistered market order.</returns>
    public static Order NewMarketOrder(this Position position, OrderSide side, decimal totalQty) =>
        new(Guid.NewGuid(), position, side, OrderType.Market, totalQty, 0, 0, 0, OrderStatus.New, 0, 0, 0, 0);

    #endregion

    #region new take profit market order

    /// <summary>Builds a new buy take-profit market order for the position.</summary>
    /// <param name="position">The position to place the order against.</param>
    /// <param name="totalQty">The total quantity the order is placed for.</param>
    /// <param name="levelPrice">The order's trigger price.</param>
    /// <returns>A new, unregistered take-profit market order.</returns>
    public static Order NewTakeProfitMarketBuyOrder(this Position position, decimal totalQty, decimal levelPrice) =>
        position.NewTakeProfitMarketOrder(OrderSide.Buy, totalQty, levelPrice);

    /// <summary>Builds a new sell take-profit market order for the position.</summary>
    /// <param name="position">The position to place the order against.</param>
    /// <param name="totalQty">The total quantity the order is placed for.</param>
    /// <param name="levelPrice">The order's trigger price.</param>
    /// <returns>A new, unregistered take-profit market order.</returns>
    public static Order NewTakeProfitMarketSellOrder(this Position position, decimal totalQty, decimal levelPrice) =>
        position.NewTakeProfitMarketOrder(OrderSide.Sell, totalQty, levelPrice);

    /// <summary>Builds a new take-profit market order for the position on the given side.</summary>
    /// <param name="position">The position to place the order against.</param>
    /// <param name="side">The side (buy or sell) to place the order on.</param>
    /// <param name="totalQty">The total quantity the order is placed for.</param>
    /// <param name="levelPrice">The order's trigger price.</param>
    /// <returns>A new, unregistered take-profit market order.</returns>
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

    /// <summary>Builds a new buy stop-loss market order for the position.</summary>
    /// <param name="position">The position to place the order against.</param>
    /// <param name="totalQty">The total quantity the order is placed for.</param>
    /// <param name="levelPrice">The order's trigger price.</param>
    /// <returns>A new, unregistered stop-loss market order.</returns>
    public static Order NewStopLossMarketBuyOrder(this Position position, decimal totalQty, decimal levelPrice) =>
        position.NewStopLossMarketOrder(OrderSide.Buy, totalQty, levelPrice);

    /// <summary>Builds a new sell stop-loss market order for the position.</summary>
    /// <param name="position">The position to place the order against.</param>
    /// <param name="totalQty">The total quantity the order is placed for.</param>
    /// <param name="levelPrice">The order's trigger price.</param>
    /// <returns>A new, unregistered stop-loss market order.</returns>
    public static Order NewStopLossMarketSellOrder(this Position position, decimal totalQty, decimal levelPrice) =>
        position.NewStopLossMarketOrder(OrderSide.Sell, totalQty, levelPrice);

    /// <summary>Builds a new stop-loss market order for the position on the given side.</summary>
    /// <param name="position">The position to place the order against.</param>
    /// <param name="side">The side (buy or sell) to place the order on.</param>
    /// <param name="totalQty">The total quantity the order is placed for.</param>
    /// <param name="levelPrice">The order's trigger price.</param>
    /// <returns>A new, unregistered stop-loss market order.</returns>
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

    /// <summary>Builds a new buy limit order for the position and registers it against it.</summary>
    /// <param name="position">The position to place the order against.</param>
    /// <param name="totalQty">The total quantity the order is placed for.</param>
    /// <param name="price">The order's limit price.</param>
    /// <returns>A result carrying the order on success, or the validation errors that blocked adding it.</returns>
    public static IResult<Order> AddLimitBuyOrder(this Position position, decimal totalQty, decimal price) =>
        position.NewLimitBuyOrder(totalQty, price).AddToPosition();

    /// <summary>Builds a new sell limit order for the position and registers it against it.</summary>
    /// <param name="position">The position to place the order against.</param>
    /// <param name="totalQty">The total quantity the order is placed for.</param>
    /// <param name="price">The order's limit price.</param>
    /// <returns>A result carrying the order on success, or the validation errors that blocked adding it.</returns>
    public static IResult<Order> AddLimitSellOrder(this Position position, decimal totalQty, decimal price) =>
        position.NewLimitSellOrder(totalQty, price).AddToPosition();

    /// <summary>Builds a new limit order for the position on the given side and registers it against it.</summary>
    /// <param name="position">The position to place the order against.</param>
    /// <param name="side">The side (buy or sell) to place the order on.</param>
    /// <param name="totalQty">The total quantity the order is placed for.</param>
    /// <param name="price">The order's limit price.</param>
    /// <returns>A result carrying the order on success, or the validation errors that blocked adding it.</returns>
    public static IResult<Order> AddLimitOrder(
        this Position position,
        OrderSide side,
        decimal totalQty,
        decimal price
    ) => position.NewLimitOrder(side, totalQty, price).AddToPosition();

    #endregion

    #region add market order

    /// <summary>Builds a new buy market order for the position and registers it against it.</summary>
    /// <param name="position">The position to place the order against.</param>
    /// <param name="totalQty">The total quantity the order is placed for.</param>
    /// <returns>A result carrying the order on success, or the validation errors that blocked adding it.</returns>
    public static IResult<Order> AddMarketBuyOrder(this Position position, decimal totalQty) =>
        position.NewMarketBuyOrder(totalQty).AddToPosition();

    /// <summary>Builds a new sell market order for the position and registers it against it.</summary>
    /// <param name="position">The position to place the order against.</param>
    /// <param name="totalQty">The total quantity the order is placed for.</param>
    /// <returns>A result carrying the order on success, or the validation errors that blocked adding it.</returns>
    public static IResult<Order> AddMarketSellOrder(this Position position, decimal totalQty) =>
        position.NewMarketSellOrder(totalQty).AddToPosition();

    /// <summary>Builds a new market order for the position on the given side and registers it against it.</summary>
    /// <param name="position">The position to place the order against.</param>
    /// <param name="side">The side (buy or sell) to place the order on.</param>
    /// <param name="totalQty">The total quantity the order is placed for.</param>
    /// <returns>A result carrying the order on success, or the validation errors that blocked adding it.</returns>
    public static IResult<Order> AddMarketOrder(this Position position, OrderSide side, decimal totalQty) =>
        position.NewMarketOrder(side, totalQty).AddToPosition();

    #endregion

    #region add take profit market order

    /// <summary>Builds a new buy take-profit market order for the position and registers it against it.</summary>
    /// <param name="position">The position to place the order against.</param>
    /// <param name="totalQty">The total quantity the order is placed for.</param>
    /// <param name="price">The order's trigger price.</param>
    /// <returns>A result carrying the order on success, or the validation errors that blocked adding it.</returns>
    public static IResult<Order> AddTakeProfitMarketBuyOrder(this Position position, decimal totalQty, decimal price) =>
        position.NewTakeProfitMarketBuyOrder(totalQty, price).AddToPosition();

    /// <summary>Builds a new sell take-profit market order for the position and registers it against it.</summary>
    /// <param name="position">The position to place the order against.</param>
    /// <param name="totalQty">The total quantity the order is placed for.</param>
    /// <param name="price">The order's trigger price.</param>
    /// <returns>A result carrying the order on success, or the validation errors that blocked adding it.</returns>
    public static IResult<Order> AddTakeProfitMarketSellOrder(
        this Position position,
        decimal totalQty,
        decimal price
    ) => position.NewTakeProfitMarketSellOrder(totalQty, price).AddToPosition();

    /// <summary>Builds a new take-profit market order for the position on the given side and registers it against it.</summary>
    /// <param name="position">The position to place the order against.</param>
    /// <param name="side">The side (buy or sell) to place the order on.</param>
    /// <param name="totalQty">The total quantity the order is placed for.</param>
    /// <param name="price">The order's trigger price.</param>
    /// <returns>A result carrying the order on success, or the validation errors that blocked adding it.</returns>
    public static IResult<Order> AddTakeProfitMarketOrder(
        this Position position,
        OrderSide side,
        decimal totalQty,
        decimal price
    ) => position.NewTakeProfitMarketOrder(side, totalQty, price).AddToPosition();

    #endregion

    #region add stop loss market order

    /// <summary>Builds a new buy stop-loss market order for the position and registers it against it.</summary>
    /// <param name="position">The position to place the order against.</param>
    /// <param name="totalQty">The total quantity the order is placed for.</param>
    /// <param name="price">The order's trigger price.</param>
    /// <returns>A result carrying the order on success, or the validation errors that blocked adding it.</returns>
    public static IResult<Order> AddStopLossMarketBuyOrder(this Position position, decimal totalQty, decimal price) =>
        position.NewStopLossMarketBuyOrder(totalQty, price).AddToPosition();

    /// <summary>Builds a new sell stop-loss market order for the position and registers it against it.</summary>
    /// <param name="position">The position to place the order against.</param>
    /// <param name="totalQty">The total quantity the order is placed for.</param>
    /// <param name="price">The order's trigger price.</param>
    /// <returns>A result carrying the order on success, or the validation errors that blocked adding it.</returns>
    public static IResult<Order> AddStopLossMarketSellOrder(this Position position, decimal totalQty, decimal price) =>
        position.NewStopLossMarketSellOrder(totalQty, price).AddToPosition();

    /// <summary>Builds a new stop-loss market order for the position on the given side and registers it against it.</summary>
    /// <param name="position">The position to place the order against.</param>
    /// <param name="side">The side (buy or sell) to place the order on.</param>
    /// <param name="totalQty">The total quantity the order is placed for.</param>
    /// <param name="price">The order's trigger price.</param>
    /// <returns>A result carrying the order on success, or the validation errors that blocked adding it.</returns>
    public static IResult<Order> AddStopLossMarketOrder(
        this Position position,
        OrderSide side,
        decimal totalQty,
        decimal price
    ) => position.NewStopLossMarketOrder(side, totalQty, price).AddToPosition();

    #endregion

    #region helpers

    /// <summary>
    /// Removes a canceled or filled order from the position, reversing whatever quantity it had contributed.
    /// </summary>
    /// <param name="position">The position to remove the order from.</param>
    /// <param name="order">The order to remove.</param>
    /// <returns>A result carrying the order on success, or the validation errors that blocked removing it.</returns>
    public static IResult<Order> RemoveOrder(this Position position, Order order)
    {
        var result = order.AsResult();
        position.RemoveOrder(
            order.Id,
            order.Side,
            order.TotalQty,
            order.Status is OrderStatus.Canceled ? order.ExecutedQty : order.TotalQty,
            order.ExecutedQty,
            order.ExecutedPrice,
            order.Fee,
            order.UpdatedAt,
            result
        );

        return result;
    }

    #endregion
}
