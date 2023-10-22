using System;

namespace Annium.Finance.Providers.Abstractions.Domain.Models;

public sealed record InstrumentTicker(
    Guid InstrumentId,
    decimal BidPrice,
    decimal AskPrice
)
{
    public override string ToString() => $"{InstrumentId}: {BidPrice} - {AskPrice}";
    public override int GetHashCode() => HashCode.Combine(InstrumentId, BidPrice, AskPrice);
    public bool Equals(InstrumentTicker? other) => GetHashCode() == other?.GetHashCode();
}