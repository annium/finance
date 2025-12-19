using System.Collections.Generic;
using Annium.Finance.Providers.Abstractions.Domain.User;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Contracts.User.Domain;

internal static class OrderStatuses
{
    public static readonly IReadOnlyDictionary<OrderStatus, string> ValueToString;
    public static readonly IReadOnlyDictionary<string, OrderStatus> StringToValue;

    static OrderStatuses()
    {
        ValueToString = new Dictionary<OrderStatus, string>
        {
            { OrderStatus.New, "NEW" },
            { OrderStatus.PartiallyFilled, "PARTIALLY_FILLED" },
            { OrderStatus.Filled, "FILLED" },
            { OrderStatus.Canceled, "CANCELED" },
            { OrderStatus.Rejected, "REJECTED" },
            { OrderStatus.Expired, "EXPIRED" },
        };

        StringToValue = new Dictionary<string, OrderStatus>
        {
            { "NEW", OrderStatus.New },
            { "PARTIALLY_FILLED", OrderStatus.PartiallyFilled },
            { "FILLED", OrderStatus.Filled },
            { "CANCELED", OrderStatus.Canceled },
            { "REJECTED", OrderStatus.Rejected },
            { "EXPIRED", OrderStatus.Expired },
            { "EXPIRED_IN_MATCH", OrderStatus.Rejected },
        };
    }
}
