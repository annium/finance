using System.Runtime.CompilerServices;
using Annium.Finance.Providers.Abstractions.Domain.Interfaces;
using static Annium.Finance.Providers.Abstractions.Domain.Enums.OrderType;
using static Annium.Finance.Providers.Abstractions.Domain.Enums.OrderStatus;

namespace Annium.Finance.Providers.Abstractions.Domain.Extensions;

public static class OrderExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsActive<TOrder>(this TOrder order)
        where TOrder : IOrder
    {
        return order.Status is New or PartiallyFilled;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsInactive<TOrder>(this TOrder order)
        where TOrder : IOrder
    {
        return order.Status is Filled or Canceled;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsImmediate<TOrder>(this TOrder order)
        where TOrder : IOrder
    {
        return order.Type is Limit or Market;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsLeveled<TOrder>(this TOrder order)
        where TOrder : IOrder
    {
        return order.Type is StopLossMarket or TakeProfitMarket or StopLossLimit or TakeProfitLimit;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsLimit<TOrder>(this TOrder order)
        where TOrder : IOrder
    {
        return order.Type is Limit or StopLossLimit or TakeProfitLimit;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsMarket<TOrder>(this TOrder order)
        where TOrder : IOrder
    {
        return order.Type is Market or StopLossMarket or TakeProfitMarket;
    }

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static decimal OpeningQty<TOrder>(this TOrder order)
        where TOrder : IOrder
    {
        return order.Status is Canceled ? 0 : order.TotalQty - order.ExecutedQty;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static decimal PotentialQty<TOrder>(this TOrder order)
        where TOrder : IOrder
    {
        return order.Status is Canceled ? order.ExecutedQty : order.TotalQty;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static decimal CancellableQty<TOrder>(this TOrder order)
        where TOrder : IOrder
    {
        return order.Status is Canceled ? order.TotalQty - order.ExecutedQty : 0;
    }
}
