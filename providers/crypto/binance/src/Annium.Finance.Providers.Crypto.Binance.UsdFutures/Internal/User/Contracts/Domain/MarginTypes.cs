using System.Collections.Generic;
using System.Linq;
using Annium.Finance.Providers.Abstractions.Domain.User;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.User.Contracts.Domain;

/// <summary>
/// Maps between <see cref="MarginType"/> and Binance's lowercase <c>marginType</c> wire values (<c>isolated</c>,
/// <c>cross</c>).
/// </summary>
internal static class MarginTypes
{
    /// <summary>Maps each <see cref="MarginType"/> to its <c>marginType</c> wire value.</summary>
    public static readonly IReadOnlyDictionary<MarginType, string> ValueToString;

    /// <summary>Maps each <c>marginType</c> wire value to its <see cref="MarginType"/>.</summary>
    public static readonly IReadOnlyDictionary<string, MarginType> StringToValue;

    /// <summary>Initializes the <see cref="ValueToString"/> and <see cref="StringToValue"/> lookup tables.</summary>
    static MarginTypes()
    {
        ValueToString = new Dictionary<MarginType, string>
        {
            { MarginType.Isolated, "isolated" },
            { MarginType.Cross, "cross" },
        };

        StringToValue = ValueToString.ToDictionary(x => x.Value, x => x.Key);
    }
}
