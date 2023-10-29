using System;

namespace Annium.Finance.Providers.Abstractions.Domain.Models;

public sealed record InstrumentTicker(string Symbol, decimal BidPrice, decimal AskPrice)
{
    public override string ToString() => $"{Symbol}: {BidPrice} - {AskPrice}";

    public override int GetHashCode() => HashCode.Combine(Symbol, BidPrice, AskPrice);

    public bool Equals(InstrumentTicker? other) => GetHashCode() == other?.GetHashCode();
}
