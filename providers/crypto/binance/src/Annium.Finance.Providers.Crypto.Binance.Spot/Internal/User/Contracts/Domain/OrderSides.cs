using System.Collections.Generic;
using System.Linq;
using Annium.Finance.Providers.Abstractions.Domain.User;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Internal.User.Contracts.Domain;

/// <summary>Maps between the library's <see cref="OrderSide"/> and the Binance <c>side</c> string values (<c>BUY</c>/<c>SELL</c>).</summary>
internal static class OrderSides
{
    /// <summary>Maps an <see cref="OrderSide"/> to its Binance wire representation.</summary>
    public static readonly IReadOnlyDictionary<OrderSide, string> ValueToString;

    /// <summary>Maps a Binance <c>side</c> string to the corresponding <see cref="OrderSide"/>.</summary>
    public static readonly IReadOnlyDictionary<string, OrderSide> StringToValue;

    /// <summary>Initializes the static lookup tables.</summary>
    static OrderSides()
    {
        ValueToString = new Dictionary<OrderSide, string> { { OrderSide.Buy, "BUY" }, { OrderSide.Sell, "SELL" } };

        StringToValue = ValueToString.ToDictionary(x => x.Value, x => x.Key);
    }
}
