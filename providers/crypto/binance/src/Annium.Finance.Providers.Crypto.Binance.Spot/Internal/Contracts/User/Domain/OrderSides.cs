using System.Collections.Generic;
using System.Linq;
using Annium.Finance.Providers.Abstractions.Domain.User;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Internal.Contracts.User.Domain;

internal static class OrderSides
{
    public static readonly IReadOnlyDictionary<OrderSide, string> ValueToString;
    public static readonly IReadOnlyDictionary<string, OrderSide> StringToValue;

    static OrderSides()
    {
        ValueToString = new Dictionary<OrderSide, string> { { OrderSide.Buy, "BUY" }, { OrderSide.Sell, "SELL" } };

        StringToValue = ValueToString.ToDictionary(x => x.Value, x => x.Key);
    }
}
