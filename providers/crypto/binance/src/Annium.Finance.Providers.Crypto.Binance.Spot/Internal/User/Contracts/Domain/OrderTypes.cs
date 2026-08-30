using System.Collections.Generic;
using Annium.Finance.Providers.Abstractions.Domain.User;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Internal.User.Contracts.Domain;

/// <summary>
/// Maps between the library's <see cref="OrderType"/> and the Binance <c>type</c> string values. On the read
/// side, Binance's <c>LIMIT_MAKER</c> is folded into <see cref="OrderType.Limit"/>.
/// </summary>
internal static class OrderTypes
{
    /// <summary>Maps an <see cref="OrderType"/> to its Binance wire representation.</summary>
    public static readonly IReadOnlyDictionary<OrderType, string> ValueToString;

    /// <summary>Maps a Binance <c>type</c> string to the corresponding <see cref="OrderType"/>.</summary>
    public static readonly IReadOnlyDictionary<string, OrderType> StringToValue;

    /// <summary>Initializes the static lookup tables.</summary>
    static OrderTypes()
    {
        ValueToString = new Dictionary<OrderType, string>
        {
            { OrderType.Limit, "LIMIT" },
            { OrderType.Market, "MARKET" },
            { OrderType.StopLossMarket, "STOP_LOSS" },
            { OrderType.TakeProfitMarket, "TAKE_PROFIT" },
            { OrderType.StopLossLimit, "STOP_LOSS_LIMIT" },
            { OrderType.TakeProfitLimit, "TAKE_PROFIT_LIMIT" },
        };

        StringToValue = new Dictionary<string, OrderType>
        {
            { "LIMIT", OrderType.Limit },
            { "MARKET", OrderType.Market },
            { "STOP_LOSS", OrderType.StopLossMarket },
            { "TAKE_PROFIT", OrderType.TakeProfitMarket },
            { "STOP_LOSS_LIMIT", OrderType.StopLossLimit },
            { "TAKE_PROFIT_LIMIT", OrderType.TakeProfitLimit },
            { "LIMIT_MAKER", OrderType.Limit },
        };
    }
}
