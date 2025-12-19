namespace Annium.Finance.Providers.Abstractions.Domain.Market;

public sealed record InstrumentTicker(string Symbol, decimal BidPrice, decimal AskPrice)
{
    public static InstrumentTicker Empty { get; } = new(string.Empty, 0, 0);

    public override string ToString() => $"{Symbol}: {BidPrice} - {AskPrice}";
}
