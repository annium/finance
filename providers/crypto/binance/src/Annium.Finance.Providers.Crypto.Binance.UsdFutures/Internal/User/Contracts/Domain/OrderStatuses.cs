using System.Collections.Generic;
using Annium.Finance.Providers.Abstractions.Domain.User;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.User.Contracts.Domain;

/// <summary>
/// Maps between <see cref="OrderStatus"/> and Binance's <c>status</c> wire values. The reverse map additionally
/// folds <c>EXPIRED_IN_MATCH</c> (order expired by the exchange's self-trade prevention) into
/// <see cref="OrderStatus.Rejected"/>, since the library has no distinct status for it.
/// </summary>
internal static class OrderStatuses
{
    /// <summary>Maps each <see cref="OrderStatus"/> to its <c>status</c> wire value.</summary>
    public static readonly IReadOnlyDictionary<OrderStatus, string> ValueToString;

    /// <summary>Maps each <c>status</c> wire value to its <see cref="OrderStatus"/>.</summary>
    public static readonly IReadOnlyDictionary<string, OrderStatus> StringToValue;

    /// <summary>Initializes the <see cref="ValueToString"/> and <see cref="StringToValue"/> lookup tables.</summary>
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
