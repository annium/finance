using System;
using System.Runtime.CompilerServices;
using Annium.Data.Operations;
using Annium.Finance.Providers.Abstractions.Domain.Enums;
using Annium.Finance.Providers.Abstractions.Domain.Interfaces;

namespace Annium.Finance.Providers.Abstractions.Domain.Extensions;

public static class OrderValidationExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IResult<TOrder> ValidateSide<TOrder>(this IResult<TOrder> result, OrderSide side)
        where TOrder : IOrder
    {
        if (result.Data.Side != side)
            result.Error($"Order {result.Data} is not a {side} order");

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IResult<TOrder> ValidateIsImmediate<TOrder>(this IResult<TOrder> result)
        where TOrder : IOrder
    {
        if (!result.Data.IsImmediate())
            result.Error($"Order {result.Data} is not an immediate order");

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IResult<TOrder> ValidateIsLeveled<TOrder>(this IResult<TOrder> result)
        where TOrder : IOrder
    {
        if (!result.Data.IsLeveled())
            result.Error($"Order {result.Data} is not a leveled order");

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IResult<TOrder> ValidateIsLimit<TOrder>(this IResult<TOrder> result)
        where TOrder : IOrder
    {
        if (!result.Data.IsLimit())
            result.Error($"Order {result.Data} is not a limit order");

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IResult<TOrder> ValidateIsMarket<TOrder>(this IResult<TOrder> result)
        where TOrder : IOrder
    {
        if (!result.Data.IsMarket())
            result.Error($"Order {result.Data} is not a market order");

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IResult<TOrder> ValidateIsActive<TOrder>(this IResult<TOrder> result)
        where TOrder : IOrder
    {
        if (result.Data.Status is not (OrderStatus.New or OrderStatus.PartiallyFilled))
            result.Error($"Order {result.Data} is not Active");

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IResult<TOrder> ValidateIsInactive<TOrder>(this IResult<TOrder> result)
        where TOrder : IOrder
    {
        if (result.Data.Status is OrderStatus.New or OrderStatus.PartiallyFilled)
            result.Error($"Order {result.Data} is not Inactive");

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IResult<TOrder> ValidateStatus<TOrder>(this IResult<TOrder> result, OrderStatus status)
        where TOrder : IOrder
    {
        if (result.Data.Status != status)
            result.Error($"Order {result.Data} is not {status}");

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IResult<TOrder> ValidateStatus<TOrder>(this IResult<TOrder> result, params OrderStatus[] statuses)
        where TOrder : IOrder
    {
        if (!Array.Exists(statuses, x => x == result.Data.Status))
            result.Error($"Order {result.Data} is not {string.Join(", ", statuses)}");

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IResult<TOrder> ValidateQtyAndPrice<TOrder>(this IResult<TOrder> result)
        where TOrder : IOrder
    {
        var order = result.Data;

        return result.ValidateQtyAndPrice(order.Status, order.ExecutedQty, order.ExecutedPrice);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IResult<TOrder> ValidateQtyAndPrice<TOrder>(
        this IResult<TOrder> result,
        OrderStatus status,
        decimal executedQty,
        decimal executedPrice
    )
        where TOrder : IOrder
    {
        var order = result.Data;

        if (order.TotalQty <= 0m)
        {
            result.Error($"Order {order} total qty is invalid");
            return result;
        }

        // for immediate order - level price must be zero
        if (order.IsImmediate())
            result.ValidateLevelPrice(PriceIsZero);

        // for leveled order - level price must be set
        if (order.IsLeveled())
            result.ValidateLevelPrice(PriceIsAboveZero);

        // for limit order - price must be set always,
        if (order.IsLimit())
            result.ValidatePrice(PriceIsAboveZero);

        // for market order - price must be zero
        if (order.IsMarket())
            result.ValidatePrice(PriceIsZero);

        return status switch
        {
            OrderStatus.New => result.ValidateNewQtyAndPrice(executedQty, executedPrice),
            OrderStatus.PartiallyFilled => result.ValidatePartiallyFilledQtyAndPrice(executedQty, executedPrice),
            OrderStatus.Filled => result.ValidateFilledQtyAndPrice(executedQty, executedPrice),
            OrderStatus.Canceled => result.ValidateCanceledQtyAndPrice(executedQty, executedPrice),
            _ => result.Error($"Order {order} has unexpected status")
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IResult<TOrder> ValidateIsExecuted<TOrder>(this IResult<TOrder> result)
        where TOrder : IOrder
    {
        if (result.Data.ExecutedQty == 0m || result.Data.ExecutedPrice == 0m)
            result.Error($"Order {result.Data} has not been executed");

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static IResult<TOrder> ValidateNewQtyAndPrice<TOrder>(
        this IResult<TOrder> result,
        decimal executedQty,
        decimal executedPrice
    )
        where TOrder : IOrder
    {
        if (executedQty != 0m)
            result.Error($"Order {result.Data} executed qty is invalid");

        if (executedPrice != 0m)
            result.Error($"Order {result.Data} executed price is invalid");

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static IResult<TOrder> ValidatePartiallyFilledQtyAndPrice<TOrder>(
        this IResult<TOrder> result,
        decimal executedQty,
        decimal executedPrice
    )
        where TOrder : IOrder
    {
        if (executedQty <= 0m || executedQty >= result.Data.TotalQty)
            result.Error($"Order {result.Data} executed qty is invalid");

        if (executedPrice <= 0m)
            result.Error($"Order {result.Data} executed price is invalid");

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static IResult<TOrder> ValidateFilledQtyAndPrice<TOrder>(
        this IResult<TOrder> result,
        decimal executedQty,
        decimal executedPrice
    )
        where TOrder : IOrder
    {
        if (executedQty != result.Data.TotalQty)
            result.Error($"Order {result.Data} executed qty is invalid");

        if (executedPrice <= 0m)
            result.Error($"Order {result.Data} executed price is invalid");

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static IResult<TOrder> ValidateCanceledQtyAndPrice<TOrder>(
        this IResult<TOrder> result,
        decimal executedQty,
        decimal executedPrice
    )
        where TOrder : IOrder
    {
        if (executedQty < 0m || executedQty >= result.Data.TotalQty)
            result.Error($"Order {result.Data} executed qty is invalid");

        if (executedQty == 0 && executedPrice != 0m)
            result.Error($"Order {result.Data} executed price is invalid");

        if (executedQty > 0 && executedPrice <= 0m)
            result.Error($"Order {result.Data} executed price is invalid");

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ValidateLevelPrice<TOrder>(this IResult<TOrder> result, Func<decimal, bool> validate)
        where TOrder : IOrder
    {
        if (!validate(result.Data.LevelPrice))
            result.Error($"Order {result.Data} level price is invalid");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ValidatePrice<TOrder>(this IResult<TOrder> result, Func<decimal, bool> validate)
        where TOrder : IOrder
    {
        if (!validate(result.Data.Price))
            result.Error($"Order {result.Data} target price is invalid");
    }

    private static readonly Func<decimal, bool> PriceIsZero = price => price == 0;
    private static readonly Func<decimal, bool> PriceIsAboveZero = price => price > 0;
}
