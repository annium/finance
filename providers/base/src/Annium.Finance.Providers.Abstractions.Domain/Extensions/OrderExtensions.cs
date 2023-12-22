using System.Runtime.CompilerServices;
using Annium.Finance.Providers.Abstractions.Domain.Enums;
using Annium.Finance.Providers.Abstractions.Domain.Interfaces;

namespace Annium.Finance.Providers.Abstractions.Domain.Extensions;

public static class OrderExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsActive<TOrder>(this TOrder order)
        where TOrder : IOrder => order.Status is OrderStatus.New or OrderStatus.PartiallyFilled;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsInactive<TOrder>(this TOrder order)
        where TOrder : IOrder => order.Status is OrderStatus.Filled or OrderStatus.Canceled;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsImmediate<TOrder>(this TOrder order)
        where TOrder : IOrder => order.Type is OrderType.Limit or OrderType.Market;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsLeveled<TOrder>(this TOrder order)
        where TOrder : IOrder => order.Type is OrderType.StopLossMarket or OrderType.TakeProfitMarket;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsLimit<TOrder>(this TOrder order)
        where TOrder : IOrder => order.Type is OrderType.Limit;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsMarket<TOrder>(this TOrder order)
        where TOrder : IOrder =>
        order.Type is OrderType.Market or OrderType.StopLossMarket or OrderType.TakeProfitMarket;

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

    // [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // public static decimal LeveragedTotalValue<TPosition, TInstrument, TResource>(this IOrder<TPosition, TInstrument, TResource> order)
    //     where TPosition : IPosition<TInstrument, TResource>
    //     where TInstrument : IInstrument<TResource>
    //     where TResource : IResource
    //     =>
    //         order.TotalQty * order.TargetPrice() * order.Position.LeveragedPart();

    // [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // public static decimal FullExpenseSum<TOrder>(this TOrder order)
    //     where TOrder : IOrderBase
    //     =>
    //         order.FullExecutedSum() + order.Fee;

    // [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // public static decimal LeveragedExpenseSum<TPosition, TInstrument, TResource>(this IOrder<TPosition, TInstrument, TResource> order)
    //     where TPosition : IPosition<TInstrument, TResource>
    //     where TInstrument : IInstrument<TResource>
    //     where TResource : IResource
    //     =>
    //         order.LeveragedExecutedSum() + order.Fee;

    // [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // public static decimal FullRevenueSum<TOrder>(this TOrder order)
    //     where TOrder : IOrderBase
    //     =>
    //         order.FullExecutedSum() - order.Fee;

    // [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // public static decimal LeveragedRevenueSum<TPosition, TInstrument, TResource>(this IOrder<TPosition, TInstrument, TResource> order)
    //     where TPosition : IPosition<TInstrument, TResource>
    //     where TInstrument : IInstrument<TResource>
    //     where TResource : IResource
    //     =>
    //         order.LeveragedExecutedSum() - order.Fee;

    // [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // public static decimal FullExecutedSum<TOrder>(this TOrder order)
    //     where TOrder : IOrderBase
    //     =>
    //         order.ExecutedQty * order.ExecutedPrice;

    // [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // public static decimal LeveragedExecutedSum<TPosition, TInstrument, TResource>(this IOrder<TPosition, TInstrument, TResource> order)
    //     where TPosition : IPosition<TInstrument, TResource>
    //     where TInstrument : IInstrument<TResource>
    //     where TResource : IResource
    //     =>
    //         order.ExecutedQty * order.ExecutedPrice * order.Position.LeveragedPart();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static decimal PotentialQty<TOrder>(this TOrder order)
        where TOrder : IOrder => order.Status is OrderStatus.Canceled ? order.ExecutedQty : order.TotalQty;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static decimal CancellableQty<TOrder>(this TOrder order)
        where TOrder : IOrder => order.Status is OrderStatus.Canceled ? order.TotalQty - order.ExecutedQty : 0;
}
