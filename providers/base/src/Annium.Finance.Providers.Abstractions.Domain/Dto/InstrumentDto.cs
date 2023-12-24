using Annium.Finance.Providers.Abstractions.Domain.Interfaces;

namespace Annium.Finance.Providers.Abstractions.Domain.Dto;

public sealed record InstrumentDto : IInstrument
{
    public string Symbol { get; init; }
    public ResourceDto Target { get; init; }
    public ResourceDto Quote { get; init; }
    public ResourceDto Currency { get; init; }
    public decimal MinQty { get; private set; }
    public decimal MaxQty { get; private set; }
    public decimal LotSize { get; private set; }
    public decimal MinPrice { get; private set; }
    public decimal MaxPrice { get; private set; }
    public decimal TickSize { get; private set; }
    public decimal MinSum { get; private set; }
    public decimal MaxSum { get; private set; }
    public int MaxOrders { get; private set; }

    public InstrumentDto(
        string symbol,
        ResourceDto target,
        ResourceDto quote,
        ResourceDto currency,
        decimal minQty,
        decimal maxQty,
        decimal lotSize,
        decimal minPrice,
        decimal maxPrice,
        decimal tickSize,
        decimal minSum,
        decimal maxSum,
        int maxOrders
    )
    {
        Symbol = symbol;
        Target = target;
        Quote = quote;
        Currency = currency;
        MinQty = minQty;
        MaxQty = maxQty;
        LotSize = lotSize;
        MinPrice = minPrice;
        MaxPrice = maxPrice;
        TickSize = tickSize;
        MinSum = minSum;
        MaxSum = maxSum;
        MaxOrders = maxOrders;
    }

    public void Update(
        decimal minQty,
        decimal maxQty,
        decimal lotSize,
        decimal minPrice,
        decimal maxPrice,
        decimal tickSize,
        decimal minSum,
        decimal maxSum,
        int maxOrders
    )
    {
        MinQty = minQty;
        MaxQty = maxQty;
        LotSize = lotSize;
        MinPrice = minPrice;
        MaxPrice = maxPrice;
        TickSize = tickSize;
        MinSum = minSum;
        MaxSum = maxSum;
        MaxOrders = maxOrders;
    }

    public override string ToString() => Symbol;
}
