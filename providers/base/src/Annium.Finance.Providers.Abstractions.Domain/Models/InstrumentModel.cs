using Annium.Finance.Providers.Abstractions.Domain.Interfaces;

namespace Annium.Finance.Providers.Abstractions.Domain.Models;

public sealed record InstrumentModel : IInstrument
{
    public string Symbol { get; init; }
    public ResourceModel Target { get; init; }
    public ResourceModel Quote { get; init; }
    public ResourceModel Currency { get; init; }
    public decimal MinQty { get; private set; }
    public decimal MaxQty { get; private set; }
    public decimal LotSize { get; private set; }
    public decimal MinPrice { get; private set; }
    public decimal MaxPrice { get; private set; }
    public decimal TickSize { get; private set; }
    public decimal MinSum { get; private set; }
    public decimal MaxSum { get; private set; }
    public int MaxOrders { get; private set; }

    public InstrumentModel(
        string symbol,
        ResourceModel target,
        ResourceModel quote,
        ResourceModel currency,
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
