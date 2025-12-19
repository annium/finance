using System.Runtime.CompilerServices;

namespace Annium.Finance.Providers.Abstractions.Domain.Market;

public static class InstrumentTickerExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static decimal Price(this InstrumentTicker ticker) => (ticker.BidPrice + ticker.AskPrice) / 2;
}
