using Annium.Finance.Providers.Abstractions.Domain.Internal.User.Requests;

namespace Annium.Finance.Providers.Abstractions.Domain.User.Requests;

/// <summary>
/// Builds <see cref="IInitOrderRequest"/>, <see cref="IModifyOrderRequest"/> and <see cref="ICancelOrderRequest"/>
/// instances for the supported order types, filling in the type-specific price and level-price defaults.
/// </summary>
public static class RequestBuilder
{
    /// <summary>Builds a request to place a limit order.</summary>
    /// <param name="id">The client-assigned identifier to place the order under.</param>
    /// <param name="range">The orientation range the order is restricted to.</param>
    /// <param name="symbol">The instrument symbol to place the order for.</param>
    /// <param name="side">The side (buy or sell) to place the order on.</param>
    /// <param name="quantity">The quantity to place the order for, in the instrument's base asset.</param>
    /// <param name="price">The limit price of the order.</param>
    /// <param name="reduceOnly">Whether the order may only reduce an existing position.</param>
    /// <returns>An <see cref="IInitOrderRequest"/> describing the limit order.</returns>
    public static IInitOrderRequest InitLimitOrder(
        string id,
        OrientationRange range,
        string symbol,
        OrderSide side,
        decimal quantity,
        decimal price,
        bool reduceOnly = false
    )
    {
        return new InitOrderRequest
        {
            Id = id,
            Range = range,
            Symbol = symbol,
            Side = side,
            Type = OrderType.Limit,
            Qty = quantity,
            Price = price,
            LevelPrice = 0m,
            ReduceOnly = reduceOnly,
        };
    }

    /// <summary>Builds a request to place a market order.</summary>
    /// <param name="id">The client-assigned identifier to place the order under.</param>
    /// <param name="range">The orientation range the order is restricted to.</param>
    /// <param name="symbol">The instrument symbol to place the order for.</param>
    /// <param name="side">The side (buy or sell) to place the order on.</param>
    /// <param name="quantity">The quantity to place the order for, in the instrument's base asset.</param>
    /// <param name="reduceOnly">Whether the order may only reduce an existing position.</param>
    /// <returns>An <see cref="IInitOrderRequest"/> describing the market order.</returns>
    public static IInitOrderRequest InitMarketOrder(
        string id,
        OrientationRange range,
        string symbol,
        OrderSide side,
        decimal quantity,
        bool reduceOnly = false
    )
    {
        return new InitOrderRequest
        {
            Id = id,
            Range = range,
            Symbol = symbol,
            Side = side,
            Type = OrderType.Market,
            Qty = quantity,
            Price = 0m,
            LevelPrice = 0m,
            ReduceOnly = reduceOnly,
        };
    }

    /// <summary>Builds a request to place a stop-loss order that executes at market price once the level price is reached.</summary>
    /// <param name="id">The client-assigned identifier to place the order under.</param>
    /// <param name="range">The orientation range the order is restricted to.</param>
    /// <param name="symbol">The instrument symbol to place the order for.</param>
    /// <param name="side">The side (buy or sell) to place the order on.</param>
    /// <param name="quantity">The quantity to place the order for, in the instrument's base asset.</param>
    /// <param name="levelPrice">The trigger price at which the order becomes a market order.</param>
    /// <param name="reduceOnly">Whether the order may only reduce an existing position.</param>
    /// <returns>An <see cref="IInitOrderRequest"/> describing the stop-loss market order.</returns>
    public static IInitOrderRequest InitStopLossMarketOrder(
        string id,
        OrientationRange range,
        string symbol,
        OrderSide side,
        decimal quantity,
        decimal levelPrice,
        bool reduceOnly = false
    )
    {
        return new InitOrderRequest
        {
            Id = id,
            Range = range,
            Symbol = symbol,
            Side = side,
            Type = OrderType.StopLossMarket,
            Qty = quantity,
            Price = 0m,
            LevelPrice = levelPrice,
            ReduceOnly = reduceOnly,
        };
    }

    /// <summary>Builds a request to place a take-profit order that executes at market price once the level price is reached.</summary>
    /// <param name="id">The client-assigned identifier to place the order under.</param>
    /// <param name="range">The orientation range the order is restricted to.</param>
    /// <param name="symbol">The instrument symbol to place the order for.</param>
    /// <param name="side">The side (buy or sell) to place the order on.</param>
    /// <param name="quantity">The quantity to place the order for, in the instrument's base asset.</param>
    /// <param name="levelPrice">The trigger price at which the order becomes a market order.</param>
    /// <param name="reduceOnly">Whether the order may only reduce an existing position.</param>
    /// <returns>An <see cref="IInitOrderRequest"/> describing the take-profit market order.</returns>
    public static IInitOrderRequest InitTakeProfitMarketOrder(
        string id,
        OrientationRange range,
        string symbol,
        OrderSide side,
        decimal quantity,
        decimal levelPrice,
        bool reduceOnly = false
    )
    {
        return new InitOrderRequest
        {
            Id = id,
            Range = range,
            Symbol = symbol,
            Side = side,
            Type = OrderType.TakeProfitMarket,
            Qty = quantity,
            Price = 0m,
            LevelPrice = levelPrice,
            ReduceOnly = reduceOnly,
        };
    }

    /// <summary>Builds a request to place a stop-loss order that becomes a limit order once the level price is reached.</summary>
    /// <param name="id">The client-assigned identifier to place the order under.</param>
    /// <param name="range">The orientation range the order is restricted to.</param>
    /// <param name="symbol">The instrument symbol to place the order for.</param>
    /// <param name="side">The side (buy or sell) to place the order on.</param>
    /// <param name="quantity">The quantity to place the order for, in the instrument's base asset.</param>
    /// <param name="price">The limit price the order executes at once triggered.</param>
    /// <param name="levelPrice">The trigger price at which the order becomes a limit order.</param>
    /// <param name="reduceOnly">Whether the order may only reduce an existing position.</param>
    /// <returns>An <see cref="IInitOrderRequest"/> describing the stop-loss limit order.</returns>
    public static IInitOrderRequest InitStopLossLimitOrder(
        string id,
        OrientationRange range,
        string symbol,
        OrderSide side,
        decimal quantity,
        decimal price,
        decimal levelPrice,
        bool reduceOnly = false
    )
    {
        return new InitOrderRequest
        {
            Id = id,
            Range = range,
            Symbol = symbol,
            Side = side,
            Type = OrderType.StopLossLimit,
            Qty = quantity,
            Price = price,
            LevelPrice = levelPrice,
            ReduceOnly = reduceOnly,
        };
    }

    /// <summary>Builds a request to place a take-profit order that becomes a limit order once the level price is reached.</summary>
    /// <param name="id">The client-assigned identifier to place the order under.</param>
    /// <param name="range">The orientation range the order is restricted to.</param>
    /// <param name="symbol">The instrument symbol to place the order for.</param>
    /// <param name="side">The side (buy or sell) to place the order on.</param>
    /// <param name="quantity">The quantity to place the order for, in the instrument's base asset.</param>
    /// <param name="price">The limit price the order executes at once triggered.</param>
    /// <param name="levelPrice">The trigger price at which the order becomes a limit order.</param>
    /// <param name="reduceOnly">Whether the order may only reduce an existing position.</param>
    /// <returns>An <see cref="IInitOrderRequest"/> describing the take-profit limit order.</returns>
    public static IInitOrderRequest InitTakeProfitLimitOrder(
        string id,
        OrientationRange range,
        string symbol,
        OrderSide side,
        decimal quantity,
        decimal price,
        decimal levelPrice,
        bool reduceOnly = false
    )
    {
        return new InitOrderRequest
        {
            Id = id,
            Range = range,
            Symbol = symbol,
            Side = side,
            Type = OrderType.TakeProfitLimit,
            Qty = quantity,
            Price = price,
            LevelPrice = levelPrice,
            ReduceOnly = reduceOnly,
        };
    }

    /// <summary>Builds a request to modify an existing order into a limit order.</summary>
    /// <param name="order">The existing order to modify.</param>
    /// <param name="side">The side (buy or sell) the modified order should have.</param>
    /// <param name="quantity">The quantity the modified order should have, in the instrument's base asset.</param>
    /// <param name="price">The limit price the modified order should have.</param>
    /// <returns>An <see cref="IModifyOrderRequest"/> describing the modification.</returns>
    public static IModifyOrderRequest ModifyToLimitOrder(
        OrderModel order,
        OrderSide side,
        decimal quantity,
        decimal price
    )
    {
        return new ModifyOrderRequest
        {
            Order = order,
            Side = side,
            Type = OrderType.Limit,
            Qty = quantity,
            Price = price,
            LevelPrice = 0m,
        };
    }

    /// <summary>Builds a request to modify an existing order into a market order.</summary>
    /// <param name="order">The existing order to modify.</param>
    /// <param name="side">The side (buy or sell) the modified order should have.</param>
    /// <param name="quantity">The quantity the modified order should have, in the instrument's base asset.</param>
    /// <returns>An <see cref="IModifyOrderRequest"/> describing the modification.</returns>
    public static IModifyOrderRequest ModifyToMarketOrder(OrderModel order, OrderSide side, decimal quantity)
    {
        return new ModifyOrderRequest
        {
            Order = order,
            Side = side,
            Type = OrderType.Market,
            Qty = quantity,
            Price = 0m,
            LevelPrice = 0m,
        };
    }

    /// <summary>Builds a request to modify an existing order into a stop-loss order that executes at market price once triggered.</summary>
    /// <param name="order">The existing order to modify.</param>
    /// <param name="side">The side (buy or sell) the modified order should have.</param>
    /// <param name="quantity">The quantity the modified order should have, in the instrument's base asset.</param>
    /// <param name="levelPrice">The trigger price at which the modified order becomes a market order.</param>
    /// <returns>An <see cref="IModifyOrderRequest"/> describing the modification.</returns>
    public static IModifyOrderRequest ModifyToStopLossMarketOrder(
        OrderModel order,
        OrderSide side,
        decimal quantity,
        decimal levelPrice
    )
    {
        return new ModifyOrderRequest
        {
            Order = order,
            Side = side,
            Type = OrderType.StopLossMarket,
            Qty = quantity,
            Price = 0m,
            LevelPrice = levelPrice,
        };
    }

    /// <summary>Builds a request to modify an existing order into a take-profit order that executes at market price once triggered.</summary>
    /// <param name="order">The existing order to modify.</param>
    /// <param name="side">The side (buy or sell) the modified order should have.</param>
    /// <param name="quantity">The quantity the modified order should have, in the instrument's base asset.</param>
    /// <param name="levelPrice">The trigger price at which the modified order becomes a market order.</param>
    /// <returns>An <see cref="IModifyOrderRequest"/> describing the modification.</returns>
    public static IModifyOrderRequest ModifyToTakeProfitMarketOrder(
        OrderModel order,
        OrderSide side,
        decimal quantity,
        decimal levelPrice
    )
    {
        return new ModifyOrderRequest
        {
            Order = order,
            Side = side,
            Type = OrderType.TakeProfitMarket,
            Qty = quantity,
            Price = 0m,
            LevelPrice = levelPrice,
        };
    }

    /// <summary>Builds a request to modify an existing order into a stop-loss order that becomes a limit order once triggered.</summary>
    /// <param name="order">The existing order to modify.</param>
    /// <param name="side">The side (buy or sell) the modified order should have.</param>
    /// <param name="quantity">The quantity the modified order should have, in the instrument's base asset.</param>
    /// <param name="price">The limit price the modified order executes at once triggered.</param>
    /// <param name="levelPrice">The trigger price at which the modified order becomes a limit order.</param>
    /// <returns>An <see cref="IModifyOrderRequest"/> describing the modification.</returns>
    public static IModifyOrderRequest ModifyToStopLossLimitOrder(
        OrderModel order,
        OrderSide side,
        decimal quantity,
        decimal price,
        decimal levelPrice
    )
    {
        return new ModifyOrderRequest
        {
            Order = order,
            Side = side,
            Type = OrderType.StopLossLimit,
            Qty = quantity,
            Price = price,
            LevelPrice = levelPrice,
        };
    }

    /// <summary>Builds a request to modify an existing order into a take-profit order that becomes a limit order once triggered.</summary>
    /// <param name="order">The existing order to modify.</param>
    /// <param name="side">The side (buy or sell) the modified order should have.</param>
    /// <param name="quantity">The quantity the modified order should have, in the instrument's base asset.</param>
    /// <param name="price">The limit price the modified order executes at once triggered.</param>
    /// <param name="levelPrice">The trigger price at which the modified order becomes a limit order.</param>
    /// <returns>An <see cref="IModifyOrderRequest"/> describing the modification.</returns>
    public static IModifyOrderRequest ModifyToTakeProfitLimitOrder(
        OrderModel order,
        OrderSide side,
        decimal quantity,
        decimal price,
        decimal levelPrice
    )
    {
        return new ModifyOrderRequest
        {
            Order = order,
            Side = side,
            Type = OrderType.TakeProfitLimit,
            Qty = quantity,
            Price = price,
            LevelPrice = levelPrice,
        };
    }

    /// <summary>Builds a request to cancel a previously placed order.</summary>
    /// <param name="id">The provider-assigned identifier of the order to cancel.</param>
    /// <param name="clientOrderId">The client-assigned identifier of the order to cancel.</param>
    /// <param name="symbol">The instrument symbol the order to cancel belongs to.</param>
    /// <returns>An <see cref="ICancelOrderRequest"/> describing the cancellation.</returns>
    public static ICancelOrderRequest CancelOrder(string id, string clientOrderId, string symbol)
    {
        return new CancelOrderRequest
        {
            Id = id,
            ClientOrderId = clientOrderId,
            Symbol = symbol,
        };
    }
}
