using System;
using System.Runtime.CompilerServices;
using Annium.Finance.Providers.Abstractions.Domain.Enums;
using Annium.Finance.Providers.Abstractions.Domain.Interfaces;

namespace Annium.Finance.Providers.Abstractions.Domain.Extensions;

public static class OrderValidationExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TOrder ValidateSide<TOrder>(this TOrder order, OrderSide side)
        where TOrder : IOrder
    {
        if (order.Side != side)
            throw new InvalidOperationException($"Order {order} is not a {side} order");

        return order;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TOrder ValidateIsImmediate<TOrder>(this TOrder order)
        where TOrder : IOrder
    {
        if (!order.IsImmediate())
            throw new InvalidOperationException($"Order {order} is not an immediate order");

        return order;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TOrder ValidateIsLeveled<TOrder>(this TOrder order)
        where TOrder : IOrder
    {
        if (!order.IsLeveled())
            throw new InvalidOperationException($"Order {order} is not a leveled order");

        return order;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TOrder ValidateIsLimit<TOrder>(this TOrder order)
        where TOrder : IOrder
    {
        if (!order.IsLimit())
            throw new InvalidOperationException($"Order {order} is not a limit order");

        return order;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TOrder ValidateIsMarket<TOrder>(this TOrder order)
        where TOrder : IOrder
    {
        if (!order.IsMarket())
            throw new InvalidOperationException($"Order {order} is not a market order");

        return order;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TOrder ValidateStatus<TOrder>(this TOrder order, OrderStatus status)
        where TOrder : IOrder
    {
        if (order.Status != status)
            throw new InvalidOperationException($"Order {order} is not {status}");

        return order;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TOrder ValidateStatus<TOrder>(this TOrder order, params OrderStatus[] statuses)
        where TOrder : IOrder
    {
        if (!Array.Exists(statuses, x => x == order.Status))
            throw new InvalidOperationException($"Order {order} is not {string.Join(", ", statuses)}");

        return order;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TOrder ValidateQtyAndPrice<TOrder>(this TOrder order)
        where TOrder : IOrder
    {
        if (order.TotalQty <= 0m)
            throw new InvalidOperationException($"Order {order} total qty is invalid");

        // for immediate order - level price must be zero
        if (order.IsImmediate())
            order.ValidateLevelPrice(PriceIsZero);

        // for leveled order - level price must be set
        if (order.IsLeveled())
            order.ValidateLevelPrice(PriceIsAboveZero);

        // for limit order - price must be set always,
        if (order.IsLimit())
            order.ValidatePrice(PriceIsAboveZero);

        // for market order - price must be zero
        if (order.IsMarket())
            order.ValidatePrice(PriceIsZero);

        return order.Status switch
        {
            OrderStatus.New => order.ValidateNewQtyAndPrice(),
            OrderStatus.PartiallyFilled => order.ValidatePartiallyFilledQtyAndPrice(),
            OrderStatus.Filled => order.ValidateFilledQtyAndPrice(),
            OrderStatus.Canceled => order.ValidateCanceledQtyAndPrice(),
            _ => throw new InvalidOperationException($"Order {order} has unexpected status")
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TOrder ValidateIsExecuted<TOrder>(this TOrder order)
        where TOrder : IOrder
    {
        if (order.ExecutedQty == 0m || order.ExecutedPrice == 0m)
            throw new InvalidOperationException($"Order {order} has not been executed");

        return order;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static TOrder ValidateNewQtyAndPrice<TOrder>(this TOrder order)
        where TOrder : IOrder
    {
        if (order.ExecutedQty != 0m)
            throw new InvalidOperationException($"Order {order} executed qty is invalid");

        if (order.ExecutedPrice != 0m)
            throw new InvalidOperationException($"Order {order} executed price is invalid");

        return order;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static TOrder ValidatePartiallyFilledQtyAndPrice<TOrder>(this TOrder order)
        where TOrder : IOrder
    {
        if (order.ExecutedQty <= 0m || order.ExecutedQty >= order.TotalQty)
            throw new InvalidOperationException($"Order {order} executed qty is invalid");

        if (order.ExecutedPrice <= 0m)
            throw new InvalidOperationException($"Order {order} executed price is invalid");

        return order;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static TOrder ValidateFilledQtyAndPrice<TOrder>(this TOrder order)
        where TOrder : IOrder
    {
        if (order.ExecutedQty != order.TotalQty)
            throw new InvalidOperationException($"Order {order} executed qty is invalid");

        if (order.ExecutedPrice <= 0m)
            throw new InvalidOperationException($"Order {order} executed price is invalid");

        return order;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static TOrder ValidateCanceledQtyAndPrice<TOrder>(this TOrder order)
        where TOrder : IOrder
    {
        if (order.ExecutedQty < 0m || order.ExecutedQty >= order.TotalQty)
            throw new InvalidOperationException($"Order {order} executed qty is invalid");

        if (order.ExecutedQty == 0 && order.ExecutedPrice != 0m)
            throw new InvalidOperationException($"Order {order} executed price is invalid");

        if (order.ExecutedQty > 0 && order.ExecutedPrice <= 0m)
            throw new InvalidOperationException($"Order {order} executed price is invalid");

        return order;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ValidateLevelPrice<TOrder>(this TOrder order, Func<decimal, bool> validate)
        where TOrder : IOrder
    {
        if (!validate(order.LevelPrice))
            throw new InvalidOperationException($"Order {order} level price is invalid");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ValidatePrice<TOrder>(this TOrder order, Func<decimal, bool> validate)
        where TOrder : IOrder
    {
        if (!validate(order.Price))
            throw new InvalidOperationException($"Order {order} target price is invalid");
    }

    private static readonly Func<decimal, bool> PriceIsZero = price => price == 0;
    private static readonly Func<decimal, bool> PriceIsAboveZero = price => price > 0;
}
