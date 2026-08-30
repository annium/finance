namespace Annium.Finance.Providers.Abstractions.Domain.Market;

/// <summary>
/// Represents the current best bid and ask price for an instrument.
/// </summary>
/// <param name="Symbol">The instrument's trading symbol.</param>
/// <param name="BidPrice">The highest price a buyer is currently willing to pay.</param>
/// <param name="AskPrice">The lowest price a seller is currently willing to accept.</param>
public sealed record InstrumentTicker(string Symbol, decimal BidPrice, decimal AskPrice)
{
    /// <summary>Gets a placeholder ticker with an empty symbol and zero prices.</summary>
    public static InstrumentTicker Empty { get; } = new(string.Empty, 0, 0);

    /// <summary>Returns the symbol and bid/ask spread as a string.</summary>
    /// <returns>A string in the form "Symbol: BidPrice - AskPrice".</returns>
    public override string ToString() => $"{Symbol}: {BidPrice} - {AskPrice}";
}
