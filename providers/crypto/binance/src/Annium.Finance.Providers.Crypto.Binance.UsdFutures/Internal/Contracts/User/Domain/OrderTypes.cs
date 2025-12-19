using System.Collections.Generic;
using Annium.Finance.Providers.Abstractions.Domain.User;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Contracts.User.Domain;

internal static class OrderTypes
{
    public static readonly IReadOnlyDictionary<OrderType, string> ValueToString;
    public static readonly IReadOnlyDictionary<string, OrderType> StringToValue;

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
