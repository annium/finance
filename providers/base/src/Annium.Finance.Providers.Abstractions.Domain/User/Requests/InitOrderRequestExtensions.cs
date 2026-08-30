using System.Runtime.CompilerServices;
using static Annium.Finance.Providers.Abstractions.Domain.User.OrderType;

namespace Annium.Finance.Providers.Abstractions.Domain.User.Requests;

/// <summary>
/// Provides classification and pricing helpers for <see cref="IInitOrderRequest"/>.
/// </summary>
public static class InitOrderRequestExtensions
{
    /// <summary>Determines whether the request would fill immediately upon acceptance (limit or market).</summary>
    /// <param name="request">The order-initiation request.</param>
    /// <returns>True if the requested order type is <see cref="OrderType.Limit"/> or <see cref="OrderType.Market"/>, false otherwise.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsImmediate(this IInitOrderRequest request)
    {
        return request.Type is Limit or OrderType.Market;
    }

    /// <summary>Determines whether the request describes a stop-loss or take-profit order, triggered at a level price.</summary>
    /// <param name="request">The order-initiation request.</param>
    /// <returns>True if the requested order type is one of the stop-loss or take-profit types, false otherwise.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsLeveled(this IInitOrderRequest request)
    {
        return request.Type is StopLossMarket or TakeProfitMarket or StopLossLimit or TakeProfitLimit;
    }

    /// <summary>Determines whether the request describes an order that executes at a specified limit price.</summary>
    /// <param name="request">The order-initiation request.</param>
    /// <returns>True if the requested order type carries a limit price, false otherwise.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsLimit(this IInitOrderRequest request)
    {
        return request.Type is Limit or StopLossLimit or TakeProfitLimit;
    }

    /// <summary>Determines whether the request describes an order that executes at the current market price.</summary>
    /// <param name="request">The order-initiation request.</param>
    /// <returns>True if the requested order type executes at market price, false otherwise.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsMarket(this IInitOrderRequest request)
    {
        return request.Type is OrderType.Market or StopLossMarket or TakeProfitMarket;
    }

    /// <summary>Gets the price the request is aimed at: the limit price for limit orders, the trigger price for leveled orders, or zero for market orders.</summary>
    /// <param name="request">The order-initiation request.</param>
    /// <returns>The limit price, the level price, or zero.</returns>
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
