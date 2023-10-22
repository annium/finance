using System.Runtime.CompilerServices;
using Annium.Finance.Providers.Abstractions.Domain.Models;

namespace Annium.Finance.Providers.Abstractions.Domain.Extensions;

public static class InstrumentTickerExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static decimal Price(this InstrumentTicker ticker) => (ticker.BidPrice + ticker.AskPrice) / 2;
}