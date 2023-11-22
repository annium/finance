using System.Collections.Generic;
using System.Linq;
using Annium.Finance.Providers.Abstractions.Domain.Enums;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Contracts.User.Domain;

internal static class OrientationRanges
{
    public static readonly IReadOnlyDictionary<OrientationRange, string> ValueToString;
    public static readonly IReadOnlyDictionary<string, OrientationRange> StringToValue;

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
