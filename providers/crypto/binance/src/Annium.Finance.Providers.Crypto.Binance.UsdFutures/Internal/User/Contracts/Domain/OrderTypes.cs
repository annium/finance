using System.Collections.Generic;
using Annium.Finance.Providers.Abstractions.Domain.User;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.User.Contracts.Domain;

/// <summary>
/// Maps between <see cref="OrderType"/> and Binance's <c>type</c> wire values. The reverse map additionally
/// folds <c>TRAILING_STOP_MARKET</c> into <see cref="OrderType.StopLossMarket"/>, since the library has no
/// distinct type for a trailing stop.
/// </summary>
internal static class OrderTypes
{
    /// <summary>Maps each <see cref="OrderType"/> to its <c>type</c> wire value.</summary>
    public static readonly IReadOnlyDictionary<OrderType, string> ValueToString;

    /// <summary>Maps each <c>type</c> wire value to its <see cref="OrderType"/>.</summary>
    public static readonly IReadOnlyDictionary<string, OrderType> StringToValue;

    /// <summary>Initializes the <see cref="ValueToString"/> and <see cref="StringToValue"/> lookup tables.</summary>
    static OrderTypes()
    {
        ValueToString = new Dictionary<OrderType, string>
        {
            { OrderType.Limit, "LIMIT" },
            { OrderType.Market, "MARKET" },
            { OrderType.StopLossMarket, "STOP_MARKET" },
            { OrderType.TakeProfitMarket, "TAKE_PROFIT_MARKET" },
            { OrderType.StopLossLimit, "STOP" },
            { OrderType.TakeProfitLimit, "TAKE_PROFIT" },
        };

        StringToValue = new Dictionary<string, OrderType>
        {
            { "LIMIT", OrderType.Limit },
            { "MARKET", OrderType.Market },
            { "STOP_MARKET", OrderType.StopLossMarket },
            { "TAKE_PROFIT_MARKET", OrderType.TakeProfitMarket },
            { "STOP", OrderType.StopLossLimit },
            { "TAKE_PROFIT", OrderType.TakeProfitLimit },
            { "TRAILING_STOP_MARKET", OrderType.StopLossMarket },
        };
    }
}
