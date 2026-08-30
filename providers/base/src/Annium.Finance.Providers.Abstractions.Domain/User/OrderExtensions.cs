using System.Runtime.CompilerServices;
using static Annium.Finance.Providers.Abstractions.Domain.User.OrderStatus;
using static Annium.Finance.Providers.Abstractions.Domain.User.OrderType;

namespace Annium.Finance.Providers.Abstractions.Domain.User;

/// <summary>
/// Provides classification and pricing helpers for <see cref="IOrder"/>.
/// </summary>
public static class OrderExtensions
{
    /// <summary>Determines whether the order is still open and eligible to be filled or canceled (new or partially filled).</summary>
    /// <typeparam name="TOrder">The order type.</typeparam>
    /// <param name="order">The order to inspect.</param>
    /// <returns>True if <see cref="IOrder.Status"/> is <see cref="OrderStatus.New"/> or <see cref="OrderStatus.PartiallyFilled"/>, false otherwise.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsActive<TOrder>(this TOrder order)
        where TOrder : IOrder
    {
        return order.Status is New or PartiallyFilled;
    }

    /// <summary>Determines whether the order has reached a terminal state (filled, canceled, rejected or expired).</summary>
    /// <typeparam name="TOrder">The order type.</typeparam>
    /// <param name="order">The order to inspect.</param>
    /// <returns>True if <see cref="IOrder.Status"/> is a terminal status, false otherwise.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsInactive<TOrder>(this TOrder order)
        where TOrder : IOrder
    {
        return order.Status is Filled or Canceled or Rejected or Expired;
    }

    /// <summary>Determines whether the order would fill immediately upon acceptance (limit or market).</summary>
    /// <typeparam name="TOrder">The order type.</typeparam>
    /// <param name="order">The order to inspect.</param>
    /// <returns>True if <see cref="IOrder.Type"/> is <see cref="OrderType.Limit"/> or <see cref="OrderType.Market"/>, false otherwise.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsImmediate<TOrder>(this TOrder order)
        where TOrder : IOrder
    {
        return order.Type is Limit or OrderType.Market;
    }

    /// <summary>Determines whether the order is a stop-loss or take-profit order, triggered at a level price.</summary>
    /// <typeparam name="TOrder">The order type.</typeparam>
    /// <param name="order">The order to inspect.</param>
    /// <returns>True if <see cref="IOrder.Type"/> is one of the stop-loss or take-profit types, false otherwise.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsLeveled<TOrder>(this TOrder order)
        where TOrder : IOrder
    {
        return order.Type is StopLossMarket or TakeProfitMarket or StopLossLimit or TakeProfitLimit;
    }

    /// <summary>Determines whether the order executes at a specified limit price.</summary>
    /// <typeparam name="TOrder">The order type.</typeparam>
    /// <param name="order">The order to inspect.</param>
    /// <returns>True if <see cref="IOrder.Type"/> carries a limit price, false otherwise.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsLimit<TOrder>(this TOrder order)
        where TOrder : IOrder
    {
        return order.Type is Limit or StopLossLimit or TakeProfitLimit;
    }

    /// <summary>Determines whether the order executes at the current market price.</summary>
    /// <typeparam name="TOrder">The order type.</typeparam>
    /// <param name="order">The order to inspect.</param>
    /// <returns>True if <see cref="IOrder.Type"/> executes at market price, false otherwise.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsMarket<TOrder>(this TOrder order)
        where TOrder : IOrder
    {
        return order.Type is OrderType.Market or StopLossMarket or TakeProfitMarket;
    }

    /// <summary>Gets the price the order is aimed at: the limit price for limit orders, the trigger price for leveled orders, or zero for market orders.</summary>
    /// <typeparam name="TOrder">The order type.</typeparam>
    /// <param name="order">The order to inspect.</param>
    /// <returns>The limit price, the level price, or zero.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static decimal TargetPrice<TOrder>(this TOrder order)
        where TOrder : IOrder
    {
        if (order.IsLimit())
            return order.Price;

        if (order.IsLeveled())
            return order.LevelPrice;

        return 0;
    }

    /// <summary>Gets the quantity still awaiting execution on an active order.</summary>
    /// <typeparam name="TOrder">The order type.</typeparam>
    /// <param name="order">The order to inspect.</param>
    /// <returns>The unfilled quantity (total minus executed) if the order is active, zero otherwise.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static decimal OpeningQty<TOrder>(this TOrder order)
        where TOrder : IOrder
    {
        return order.IsActive() ? order.TotalQty - order.ExecutedQty : 0;
    }
}
