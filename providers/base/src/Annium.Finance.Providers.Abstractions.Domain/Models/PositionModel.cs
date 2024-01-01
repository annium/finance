using Annium.Finance.Providers.Abstractions.Domain.Enums;
using Annium.Finance.Providers.Abstractions.Domain.Interfaces;

namespace Annium.Finance.Providers.Abstractions.Domain.Models;

public sealed record PositionModel : IPosition
{
    public string Symbol { get; }
    public OrientationRange OrientationRange { get; }
    public MarginType MarginType { get; private set; }
    public decimal Leverage { get; private set; }
    public decimal Amount { get; private set; }

    public PositionModel(
        string symbol,
        OrientationRange orientationRange,
        MarginType marginType,
        decimal leverage,
        decimal amount
    )
    {
        Symbol = symbol;
        OrientationRange = orientationRange;
        MarginType = marginType;
        Leverage = leverage;
        Amount = amount;
    }

    public void Update(MarginType marginType, decimal leverage, decimal amount)
    {
        MarginType = marginType;
        Leverage = leverage;
        Amount = amount;
    }
}
