using System.Runtime.CompilerServices;
using static Annium.Finance.Providers.Abstractions.Domain.User.OrderType;

namespace Annium.Finance.Providers.Abstractions.Domain.User.Requests;

public static class InitOrderRequestExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsImmediate(this IInitOrderRequest request)
    {
        return request.Type is Limit or OrderType.Market;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsLeveled(this IInitOrderRequest request)
    {
        return request.Type is StopLossMarket or TakeProfitMarket or StopLossLimit or TakeProfitLimit;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsLimit(this IInitOrderRequest request)
    {
        return request.Type is Limit or StopLossLimit or TakeProfitLimit;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsMarket(this IInitOrderRequest request)
    {
        return request.Type is OrderType.Market or StopLossMarket or TakeProfitMarket;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static decimal TargetPrice(this IInitOrderRequest request)
    {
        if (request.IsLimit())
            return request.Price;

        if (request.IsLeveled())
            return request.LevelPrice;

        return 0;
    }
}
