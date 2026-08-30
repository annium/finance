using System;
using System.Runtime.CompilerServices;
using Annium.Data.Operations;
using static Annium.Finance.Providers.Abstractions.Domain.User.OrderStatus;

namespace Annium.Finance.Providers.Abstractions.Domain.User;

/// <summary>
/// Provides fluent validation checks for order results, appending errors to the result when the order fails a check.
/// </summary>
public static class OrderValidationExtensions
{
    /// <summary>Validates that the order was placed on the given side.</summary>
    /// <typeparam name="TOrder">The order type.</typeparam>
    /// <param name="result">The result carrying the order to validate.</param>
    /// <param name="side">The side the order is expected to be on.</param>
    /// <returns>The same result, with an error added if the order's side does not match.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IResult<TOrder> ValidateSide<TOrder>(this IResult<TOrder> result, OrderSide side)
        where TOrder : IOrder
    {
        if (result.Data.Side != side)
            result.Error($"Order {result.Data} is not a {side} order");

        return result;
    }

    /// <summary>Validates that the order fills immediately upon acceptance (limit or market).</summary>
    /// <typeparam name="TOrder">The order type.</typeparam>
    /// <param name="result">The result carrying the order to validate.</param>
    /// <returns>The same result, with an error added if the order is not an immediate order.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IResult<TOrder> ValidateIsImmediate<TOrder>(this IResult<TOrder> result)
        where TOrder : IOrder
    {
        if (!result.Data.IsImmediate())
            result.Error($"Order {result.Data} is not an immediate order");

        return result;
    }

    /// <summary>Validates that the order is a stop-loss or take-profit order, triggered at a level price.</summary>
    /// <typeparam name="TOrder">The order type.</typeparam>
    /// <param name="result">The result carrying the order to validate.</param>
    /// <returns>The same result, with an error added if the order is not a leveled order.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IResult<TOrder> ValidateIsLeveled<TOrder>(this IResult<TOrder> result)
        where TOrder : IOrder
    {
        if (!result.Data.IsLeveled())
            result.Error($"Order {result.Data} is not a leveled order");

        return result;
    }

    /// <summary>Validates that the order executes at a specified limit price.</summary>
    /// <typeparam name="TOrder">The order type.</typeparam>
    /// <param name="result">The result carrying the order to validate.</param>
    /// <returns>The same result, with an error added if the order is not a limit order.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IResult<TOrder> ValidateIsLimit<TOrder>(this IResult<TOrder> result)
        where TOrder : IOrder
    {
        if (!result.Data.IsLimit())
            result.Error($"Order {result.Data} is not a limit order");

        return result;
    }

    /// <summary>Validates that the order executes at the current market price.</summary>
    /// <typeparam name="TOrder">The order type.</typeparam>
    /// <param name="result">The result carrying the order to validate.</param>
    /// <returns>The same result, with an error added if the order is not a market order.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IResult<TOrder> ValidateIsMarket<TOrder>(this IResult<TOrder> result)
        where TOrder : IOrder
    {
        if (!result.Data.IsMarket())
            result.Error($"Order {result.Data} is not a market order");

        return result;
    }

    /// <summary>Validates that the order is still open (new or partially filled).</summary>
    /// <typeparam name="TOrder">The order type.</typeparam>
    /// <param name="result">The result carrying the order to validate.</param>
    /// <returns>The same result, with an error added if the order is not active.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IResult<TOrder> ValidateIsActive<TOrder>(this IResult<TOrder> result)
        where TOrder : IOrder
    {
        if (result.Data.Status is not (New or PartiallyFilled))
            result.Error($"Order {result.Data} is not Active");

        return result;
    }

    /// <summary>Validates that the order has reached a terminal state.</summary>
    /// <typeparam name="TOrder">The order type.</typeparam>
    /// <param name="result">The result carrying the order to validate.</param>
    /// <returns>The same result, with an error added if the order is still active.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IResult<TOrder> ValidateIsInactive<TOrder>(this IResult<TOrder> result)
        where TOrder : IOrder
    {
        if (result.Data.Status is New or PartiallyFilled)
            result.Error($"Order {result.Data} is not Inactive");

        return result;
    }

    /// <summary>Validates that the order's own status, executed quantity and executed price are internally consistent for that status.</summary>
    /// <typeparam name="TOrder">The order type.</typeparam>
    /// <param name="result">The result carrying the order to validate.</param>
    /// <returns>The same result, with errors added for any inconsistency found.</returns>
    public static IResult<TOrder> ValidateCanProcess<TOrder>(this IResult<TOrder> result)
        where TOrder : IOrder
    {
        var order = result.Data;

        return result.ValidateCanProcess(order.Status, order.ExecutedQty, order.ExecutedPrice);
    }

    /// <summary>Validates that an order's terms, plus a candidate status, executed quantity and executed price, are internally consistent (e.g. as reported by a fill or status update).</summary>
    /// <typeparam name="TOrder">The order type.</typeparam>
    /// <param name="result">The result carrying the order to validate.</param>
    /// <param name="status">The candidate status to validate the order's transition into.</param>
    /// <param name="executedQty">The candidate executed quantity to validate.</param>
    /// <param name="executedPrice">The candidate executed price to validate.</param>
    /// <returns>The same result, with errors added for any inconsistency found.</returns>
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

    /// <summary>Validates that the order has the given status.</summary>
    /// <typeparam name="TOrder">The order type.</typeparam>
    /// <param name="result">The result carrying the order to validate.</param>
    /// <param name="status">The status the order is expected to have.</param>
    /// <returns>The same result, with an error added if the order's status does not match.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IResult<TOrder> ValidateStatus<TOrder>(this IResult<TOrder> result, OrderStatus status)
        where TOrder : IOrder
    {
        if (result.Data.Status != status)
            result.Error($"Order {result.Data} is not {status}");

        return result;
    }

    /// <summary>Validates that the order has one of the given statuses.</summary>
    /// <typeparam name="TOrder">The order type.</typeparam>
    /// <param name="result">The result carrying the order to validate.</param>
    /// <param name="status1">The first acceptable status.</param>
    /// <param name="status2">The second acceptable status.</param>
    /// <returns>The same result, with an error added if the order's status matches none of the given statuses.</returns>
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

    /// <summary>Validates that the order has one of the given statuses.</summary>
    /// <typeparam name="TOrder">The order type.</typeparam>
    /// <param name="result">The result carrying the order to validate.</param>
    /// <param name="status1">The first acceptable status.</param>
    /// <param name="status2">The second acceptable status.</param>
    /// <param name="status3">The third acceptable status.</param>
    /// <returns>The same result, with an error added if the order's status matches none of the given statuses.</returns>
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

    /// <summary>Validates that the order has one of the given statuses.</summary>
    /// <typeparam name="TOrder">The order type.</typeparam>
    /// <param name="result">The result carrying the order to validate.</param>
    /// <param name="status1">The first acceptable status.</param>
    /// <param name="status2">The second acceptable status.</param>
    /// <param name="status3">The third acceptable status.</param>
    /// <param name="status4">The fourth acceptable status.</param>
    /// <returns>The same result, with an error added if the order's status matches none of the given statuses.</returns>
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

    /// <summary>Validates that the order has received at least one fill (non-zero executed quantity and price).</summary>
    /// <typeparam name="TOrder">The order type.</typeparam>
    /// <param name="result">The result carrying the order to validate.</param>
    /// <returns>The same result, with an error added if the order has not been executed.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IResult<TOrder> ValidateIsExecuted<TOrder>(this IResult<TOrder> result)
        where TOrder : IOrder
    {
        var order = result.Data;
        if (order.ExecutedQty == 0m || order.ExecutedPrice == 0m)
            result.Error($"Order {order} has not been executed");

        return result;
    }

    /// <summary>Validates the candidate executed quantity and price for an order transitioning into the New status: both must be zero.</summary>
    /// <typeparam name="TOrder">The order type.</typeparam>
    /// <param name="result">The result carrying the order to validate.</param>
    /// <param name="executedQty">The candidate executed quantity to validate.</param>
    /// <param name="executedPrice">The candidate executed price to validate.</param>
    /// <returns>The same result, with errors added for any inconsistency found.</returns>
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

    /// <summary>Validates the candidate executed quantity and price for an order transitioning into the PartiallyFilled status: quantity must have grown but stay below the total, and price must be positive.</summary>
    /// <typeparam name="TOrder">The order type.</typeparam>
    /// <param name="result">The result carrying the order to validate.</param>
    /// <param name="executedQty">The candidate executed quantity to validate.</param>
    /// <param name="executedPrice">The candidate executed price to validate.</param>
    /// <returns>The same result, with errors added for any inconsistency found.</returns>
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

    /// <summary>Validates the candidate executed quantity and price for an order transitioning into the Filled status: quantity must equal the total, and price must be positive.</summary>
    /// <typeparam name="TOrder">The order type.</typeparam>
    /// <param name="result">The result carrying the order to validate.</param>
    /// <param name="executedQty">The candidate executed quantity to validate.</param>
    /// <param name="executedPrice">The candidate executed price to validate.</param>
    /// <returns>The same result, with errors added for any inconsistency found.</returns>
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

    /// <summary>Validates the candidate executed quantity and price for an order transitioning into a declined status (canceled, rejected or expired): quantity must not have shrunk or reached the total, and price must be zero exactly when quantity is zero.</summary>
    /// <typeparam name="TOrder">The order type.</typeparam>
    /// <param name="result">The result carrying the order to validate.</param>
    /// <param name="executedQty">The candidate executed quantity to validate.</param>
    /// <param name="executedPrice">The candidate executed price to validate.</param>
    /// <returns>The same result, with errors added for any inconsistency found.</returns>
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

    /// <summary>Validates the order's level price against a predicate, adding an error if it fails.</summary>
    /// <typeparam name="TOrder">The order type.</typeparam>
    /// <param name="result">The result carrying the order to validate.</param>
    /// <param name="validate">The predicate the level price must satisfy.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ValidateLevelPrice<TOrder>(this IResult<TOrder> result, Func<decimal, bool> validate)
        where TOrder : IOrder
    {
        if (!validate(result.Data.LevelPrice))
            result.Error($"Order {result.Data} level price is invalid");
    }

    /// <summary>Validates the order's target price against a predicate, adding an error if it fails.</summary>
    /// <typeparam name="TOrder">The order type.</typeparam>
    /// <param name="result">The result carrying the order to validate.</param>
    /// <param name="validate">The predicate the target price must satisfy.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ValidatePrice<TOrder>(this IResult<TOrder> result, Func<decimal, bool> validate)
        where TOrder : IOrder
    {
        if (!validate(result.Data.Price))
            result.Error($"Order {result.Data} target price is invalid");
    }

    /// <summary>Predicate matching a price of exactly zero, used to validate immediate orders' level price and market orders' target price.</summary>
    private static readonly Func<decimal, bool> _priceIsZero = price => price == 0;

    /// <summary>Predicate matching a strictly positive price, used to validate leveled orders' level price and limit orders' target price.</summary>
    private static readonly Func<decimal, bool> _priceIsAboveZero = price => price > 0;
}
