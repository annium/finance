using System;
using System.Runtime.CompilerServices;
using Annium.Data.Operations;
using Annium.Finance.Providers.Abstractions.Domain.Enums;
using Annium.Finance.Providers.Abstractions.Domain.Interfaces;
using static Annium.Finance.Providers.Abstractions.Domain.Enums.OrderStatus;

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
        if (result.Data.Status is not (New or PartiallyFilled))
            result.Error($"Order {result.Data} is not Active");

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IResult<TOrder> ValidateIsInactive<TOrder>(this IResult<TOrder> result)
        where TOrder : IOrder
    {
        if (result.Data.Status is New or PartiallyFilled)
            result.Error($"Order {result.Data} is not Inactive");

        return result;
    }

    public static IResult<TOrder> ValidateCanProcess<TOrder>(this IResult<TOrder> result)
        where TOrder : IOrder
    {
        var order = result.Data;

        return result.ValidateCanProcess(order.Status, order.ExecutedQty, order.ExecutedPrice);
    }

    public static IResult<TOrder> ValidateCanProcess<TOrder>(
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
            result.ValidateLevelPrice(_priceIsZero);

        // for leveled order - level price must be set
        if (order.IsLeveled())
            result.ValidateLevelPrice(_priceIsAboveZero);

        // for limit order - price must be set always,
        if (order.IsLimit())
            result.ValidatePrice(_priceIsAboveZero);

        // for market order - price must be zero
        if (order.IsMarket())
            result.ValidatePrice(_priceIsZero);

        return status switch
        {
            New => result.ValidateStatus(New).ValidateNewQtyAndPrice(executedQty, executedPrice),
            PartiallyFilled => result
                .ValidateStatus(New, PartiallyFilled)
                .ValidatePartiallyFilledQtyAndPrice(executedQty, executedPrice),
            Filled => result
                .ValidateStatus(New, PartiallyFilled, Filled)
                .ValidateFilledQtyAndPrice(executedQty, executedPrice),
            Canceled => result
                .ValidateStatus(New, PartiallyFilled, Canceled)
                .ValidateDeclinedQtyAndPrice(executedQty, executedPrice),
            Rejected => result
                .ValidateStatus(New, PartiallyFilled, Rejected)
                .ValidateDeclinedQtyAndPrice(executedQty, executedPrice),
            Expired => result
                .ValidateStatus(New, PartiallyFilled, Expired)
                .ValidateDeclinedQtyAndPrice(executedQty, executedPrice),
            _ => result.Error($"Unexpected status {status}"),
        };
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
    public static IResult<TOrder> ValidateStatus<TOrder>(
        this IResult<TOrder> result,
        OrderStatus status1,
        OrderStatus status2
    )
        where TOrder : IOrder
    {
        var s = result.Data.Status;
        if (s != status1 && s != status2)
            result.Error($"Order {result.Data} is not {status1}, {status2}");

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IResult<TOrder> ValidateStatus<TOrder>(
        this IResult<TOrder> result,
        OrderStatus status1,
        OrderStatus status2,
        OrderStatus status3
    )
        where TOrder : IOrder
    {
        var s = result.Data.Status;
        if (s != status1 && s != status2 && s != status3)
            result.Error($"Order {result.Data} is not {status1}, {status2}, {status3}");

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IResult<TOrder> ValidateStatus<TOrder>(
        this IResult<TOrder> result,
        OrderStatus status1,
        OrderStatus status2,
        OrderStatus status3,
        OrderStatus status4
    )
        where TOrder : IOrder
    {
        var s = result.Data.Status;
        if (s != status1 && s != status2 && s != status3 && s != status4)
            result.Error($"Order {result.Data} is not {status1}, {status2}, {status3}, {status4}");

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IResult<TOrder> ValidateIsExecuted<TOrder>(this IResult<TOrder> result)
        where TOrder : IOrder
    {
        var order = result.Data;
        if (order.ExecutedQty == 0m || order.ExecutedPrice == 0m)
            result.Error($"Order {order} has not been executed");

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
        var order = result.Data;

        if (executedQty != 0m)
            result.Error($"Order {order} executed qty is invalid");

        if (executedPrice != 0m)
            result.Error($"Order {order} executed price is invalid");

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
        var order = result.Data;

        if (executedQty <= 0 || executedQty < order.ExecutedQty || executedQty >= order.TotalQty)
            result.Error($"Order {order} executed qty is invalid");

        if (executedPrice <= 0m)
            result.Error($"Order {order} executed price is invalid");

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
        var order = result.Data;

        if (executedQty != order.TotalQty)
            result.Error($"Order {order} executed qty is invalid");

        if (executedPrice <= 0m)
            result.Error($"Order {order} executed price is invalid");

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static IResult<TOrder> ValidateDeclinedQtyAndPrice<TOrder>(
        this IResult<TOrder> result,
        decimal executedQty,
        decimal executedPrice
    )
        where TOrder : IOrder
    {
        var order = result.Data;

        if (executedQty < 0m || executedQty < order.ExecutedQty || executedQty >= order.TotalQty)
            result.Error($"Order {order} executed qty is invalid");

        if (executedQty == 0 && executedPrice != 0m)
            result.Error($"Order {order} executed price is invalid");

        if (executedQty > 0 && executedPrice <= 0m)
            result.Error($"Order {order} executed price is invalid");

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

    private static readonly Func<decimal, bool> _priceIsZero = price => price == 0;
    private static readonly Func<decimal, bool> _priceIsAboveZero = price => price > 0;
}
