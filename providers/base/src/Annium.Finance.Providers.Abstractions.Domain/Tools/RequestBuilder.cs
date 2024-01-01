using Annium.Finance.Providers.Abstractions.Domain.Enums;
using Annium.Finance.Providers.Abstractions.Domain.Interfaces;
using Annium.Finance.Providers.Abstractions.Domain.Internal.Models;
using Annium.Finance.Providers.Abstractions.Domain.Models;

namespace Annium.Finance.Providers.Abstractions.Domain.Tools;

public static class RequestBuilder
{
    public static IInitOrderRequest InitLimitOrder(
        string id,
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
            Symbol = symbol,
            Side = side,
            Type = OrderType.Limit,
            Qty = quantity,
            Price = price,
            LevelPrice = 0m,
            ReduceOnly = reduceOnly,
        };
    }

    public static IInitOrderRequest InitMarketOrder(
        string id,
        string symbol,
        OrderSide side,
        decimal quantity,
        bool reduceOnly = false
    )
    {
        return new InitOrderRequest
        {
            Id = id,
            Symbol = symbol,
            Side = side,
            Type = OrderType.Market,
            Qty = quantity,
            Price = 0m,
            LevelPrice = 0m,
            ReduceOnly = reduceOnly,
        };
    }

    public static IInitOrderRequest InitStopLossMarketOrder(
        string id,
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
            Symbol = symbol,
            Side = side,
            Type = OrderType.StopLossMarket,
            Qty = quantity,
            Price = 0m,
            LevelPrice = levelPrice,
            ReduceOnly = reduceOnly,
        };
    }

    public static IInitOrderRequest InitTakeProfitMarketOrder(
        string id,
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
            Symbol = symbol,
            Side = side,
            Type = OrderType.TakeProfitMarket,
            Qty = quantity,
            Price = 0m,
            LevelPrice = levelPrice,
            ReduceOnly = reduceOnly,
        };
    }

    public static IInitOrderRequest InitStopLossLimitOrder(
        string id,
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
            Symbol = symbol,
            Side = side,
            Type = OrderType.StopLossLimit,
            Qty = quantity,
            Price = price,
            LevelPrice = levelPrice,
            ReduceOnly = reduceOnly,
        };
    }

    public static IInitOrderRequest InitTakeProfitLimitOrder(
        string id,
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
            Symbol = symbol,
            Side = side,
            Type = OrderType.TakeProfitLimit,
            Qty = quantity,
            Price = price,
            LevelPrice = levelPrice,
            ReduceOnly = reduceOnly,
        };
    }

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
}
