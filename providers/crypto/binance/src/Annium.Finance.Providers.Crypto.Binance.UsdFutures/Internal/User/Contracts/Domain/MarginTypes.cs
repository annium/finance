using System.Collections.Generic;
using System.Linq;
using Annium.Finance.Providers.Abstractions.Domain.User;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.User.Contracts.Domain;

internal static class MarginTypes
{
    public static readonly IReadOnlyDictionary<MarginType, string> ValueToString;
    public static readonly IReadOnlyDictionary<string, MarginType> StringToValue;

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
