namespace Annium.Finance.Providers.Abstractions.Domain.Models;

public sealed record InstrumentTicker
{
    public string Symbol { get; init; }
    public decimal BidPrice { get; private set; }
    public decimal AskPrice { get; private set; }

    public InstrumentTicker(string symbol, decimal bidPrice, decimal askPrice)
    {
        Symbol = symbol;
        BidPrice = bidPrice;
        AskPrice = askPrice;
    }

    public void Update(decimal bidPrice, decimal askPrice)
    {
        BidPrice = bidPrice;
        AskPrice = askPrice;
    }

    public override string ToString() => $"{Symbol}: {BidPrice} - {AskPrice}";
}
