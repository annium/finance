using System.Collections.Generic;
using System.Linq;
using Annium.Finance.Providers.Abstractions.Domain.User;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.User.Contracts.Domain;

/// <summary>
/// Maps between <see cref="OrientationRange"/> and Binance's <c>positionSide</c> wire values (<c>BOTH</c>,
/// <c>LONG</c>, <c>SHORT</c>), used for position side across orders, positions and user data stream events.
/// </summary>
internal static class OrientationRanges
{
    /// <summary>Maps each <see cref="OrientationRange"/> to its <c>positionSide</c> wire value.</summary>
    public static readonly IReadOnlyDictionary<OrientationRange, string> ValueToString;

    /// <summary>Maps each <c>positionSide</c> wire value to its <see cref="OrientationRange"/>.</summary>
    public static readonly IReadOnlyDictionary<string, OrientationRange> StringToValue;

    /// <summary>Initializes the <see cref="ValueToString"/> and <see cref="StringToValue"/> lookup tables.</summary>
    static OrientationRanges()
    {
        ValueToString = new Dictionary<OrientationRange, string>
        {
            { OrientationRange.Both, "BOTH" },
            { OrientationRange.Long, "LONG" },
            { OrientationRange.Short, "SHORT" },
        };

        StringToValue = ValueToString.ToDictionary(x => x.Value, x => x.Key);
    }
}
