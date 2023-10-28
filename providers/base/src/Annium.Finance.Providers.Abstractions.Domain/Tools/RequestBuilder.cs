using Annium.Finance.Providers.Abstractions.Domain.Enums;
using Annium.Finance.Providers.Abstractions.Domain.Interfaces;
using Annium.Finance.Providers.Abstractions.Domain.Internal.Models;

namespace Annium.Finance.Providers.Abstractions.Domain.Tools;

public static class RequestBuilder
{
    public static IInitOrderRequest InitLimitOrder(
        string clientOrderId,
        ISecurityKey securityKey,
        OrderSide side,
        decimal quantity,
        decimal price,
        bool reduceOnly = false
    )
    {
        return new InitOrderRequest
        {
            ClientOrderId = clientOrderId,
            SecurityKey = securityKey,
            Side = side,
            Type = OrderType.Limit,
            Quantity = quantity,
            Price = price,
            ReduceOnly = reduceOnly,
        };
    }

    public static IInitOrderRequest InitMarketOrder(
        string clientOrderId,
        ISecurityKey securityKey,
        OrderSide side,
        decimal quantity,
        bool reduceOnly = false
    )
    {
        return new InitOrderRequest
        {
            ClientOrderId = clientOrderId,
            SecurityKey = securityKey,
            Side = side,
            Type = OrderType.Market,
            Quantity = quantity,
            ReduceOnly = reduceOnly,
        };
    }

    public static IInitOrderRequest InitStopLossMarketOrder(
        string clientOrderId,
        ISecurityKey securityKey,
        OrderSide side,
        decimal quantity,
        decimal triggerPrice,
        bool reduceOnly = false
    )
    {
        return new InitOrderRequest
        {
            ClientOrderId = clientOrderId,
            SecurityKey = securityKey,
            Side = side,
            Type = OrderType.StopLossMarket,
            Quantity = quantity,
            TriggerPrice = triggerPrice,
            ReduceOnly = reduceOnly,
        };
    }

    public static IInitOrderRequest InitTakeProfitMarketOrder(
        string clientOrderId,
        ISecurityKey securityKey,
        OrderSide side,
        decimal quantity,
        decimal triggerPrice,
        bool reduceOnly = false
    )
    {
        return new InitOrderRequest
        {
            ClientOrderId = clientOrderId,
            SecurityKey = securityKey,
            Side = side,
            Type = OrderType.TakeProfitMarket,
            Quantity = quantity,
            TriggerPrice = triggerPrice,
            ReduceOnly = reduceOnly,
        };
    }

    public static IInitOrderRequest InitStopLossLimitOrder(
        string clientOrderId,
        ISecurityKey securityKey,
        OrderSide side,
        decimal quantity,
        decimal price,
        decimal triggerPrice,
        bool reduceOnly = false
    )
    {
        return new InitOrderRequest
        {
            ClientOrderId = clientOrderId,
            SecurityKey = securityKey,
            Side = side,
            Type = OrderType.StopLossLimit,
            Quantity = quantity,
            Price = price,
            TriggerPrice = triggerPrice,
            ReduceOnly = reduceOnly,
        };
    }

    public static IInitOrderRequest InitTakeProfitLimitOrder(
        string clientOrderId,
        ISecurityKey securityKey,
        OrderSide side,
        decimal quantity,
        decimal price,
        decimal triggerPrice,
        bool reduceOnly = false
    )
    {
        return new InitOrderRequest
        {
            ClientOrderId = clientOrderId,
            SecurityKey = securityKey,
            Side = side,
            Type = OrderType.TakeProfitLimit,
            Quantity = quantity,
            Price = price,
            TriggerPrice = triggerPrice,
            ReduceOnly = reduceOnly,
        };
    }

    public static IModifyOrderRequest ModifyToLimitOrder(IOrder order, OrderSide side, decimal quantity, decimal price)
    {
        return new ModifyOrderRequest
        {
            Order = order,
            Side = side,
            Type = OrderType.Limit,
            Quantity = quantity,
            Price = price,
        };
    }

    public static IModifyOrderRequest ModifyToMarketOrder(IOrder order, OrderSide side, decimal quantity)
    {
        return new ModifyOrderRequest
        {
            Order = order,
            Side = side,
            Type = OrderType.Market,
            Quantity = quantity,
        };
    }

    public static IModifyOrderRequest ModifyToStopLossMarketOrder(
        IOrder order,
        OrderSide side,
        decimal quantity,
        decimal triggerPrice
    )
    {
        return new ModifyOrderRequest
        {
            Order = order,
            Side = side,
            Type = OrderType.StopLossMarket,
            Quantity = quantity,
            TriggerPrice = triggerPrice,
        };
    }

    public static IModifyOrderRequest ModifyToTakeProfitMarketOrder(
        IOrder order,
        OrderSide side,
        decimal quantity,
        decimal triggerPrice
    )
    {
        return new ModifyOrderRequest
        {
            Order = order,
            Side = side,
            Type = OrderType.TakeProfitMarket,
            Quantity = quantity,
            TriggerPrice = triggerPrice,
        };
    }

    public static IModifyOrderRequest ModifyToStopLossLimitOrder(
        IOrder order,
        OrderSide side,
        decimal quantity,
        decimal price,
        decimal triggerPrice
    )
    {
        return new ModifyOrderRequest
        {
            Order = order,
            Side = side,
            Type = OrderType.StopLossLimit,
            Quantity = quantity,
            Price = price,
            TriggerPrice = triggerPrice,
        };
    }

    public static IModifyOrderRequest ModifyToTakeProfitLimitOrder(
        IOrder order,
        OrderSide side,
        decimal quantity,
        decimal price,
        decimal triggerPrice
    )
    {
        return new ModifyOrderRequest
        {
            Order = order,
            Side = side,
            Type = OrderType.TakeProfitLimit,
            Quantity = quantity,
            Price = price,
            TriggerPrice = triggerPrice,
        };
    }
}
