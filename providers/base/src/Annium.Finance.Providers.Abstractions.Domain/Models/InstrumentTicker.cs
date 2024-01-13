namespace Annium.Finance.Providers.Abstractions.Domain.Models;

public sealed record InstrumentTicker(string Symbol, decimal BidPrice, decimal AskPrice)
{
    public override string ToString() => $"{Symbol}: {BidPrice} - {AskPrice}";
}
