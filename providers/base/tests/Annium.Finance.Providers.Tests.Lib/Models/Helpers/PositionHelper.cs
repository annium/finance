using System.Runtime.CompilerServices;

namespace Annium.Finance.Providers.Tests.Lib.Models.Helpers;

public static class PositionHelper
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static decimal ResolvePrice(
        decimal currentQty,
        decimal currentPrice,
        decimal executedQty,
        decimal executedPrice
    )
    {
        var totalQty = currentQty + executedQty;
        if (totalQty == 0)
            return 0;

        return (currentQty * currentPrice + executedQty * executedPrice) / totalQty;
    }
}
