using System.Collections.Generic;
using System.Linq;
using Annium.Finance.Providers.Abstractions.Domain.User;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.User.Contracts.Domain;

/// <summary>
/// Maps between <see cref="OrderSide"/> and Binance's <c>side</c> wire values (<c>BUY</c>, <c>SELL</c>).
/// </summary>
internal static class OrderSides
{
    /// <summary>Maps each <see cref="OrderSide"/> to its <c>side</c> wire value.</summary>
    public static readonly IReadOnlyDictionary<OrderSide, string> ValueToString;

    /// <summary>Maps each <c>side</c> wire value to its <see cref="OrderSide"/>.</summary>
    public static readonly IReadOnlyDictionary<string, OrderSide> StringToValue;

    /// <summary>Initializes the <see cref="ValueToString"/> and <see cref="StringToValue"/> lookup tables.</summary>
    static OrderSides()
    {
        ValueToString = new Dictionary<OrderSide, string> { { OrderSide.Buy, "BUY" }, { OrderSide.Sell, "SELL" } };

        StringToValue = ValueToString.ToDictionary(x => x.Value, x => x.Key);
    }
}
