using System.Collections.Generic;
using Annium.Finance.Providers.Abstractions.Domain.User;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Internal.Contracts.User.Domain;

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
