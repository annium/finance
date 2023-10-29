namespace Annium.Finance.Providers.Crypto.Binance.Base.Internal.Contracts.Market.Domain;

internal class Filters
{
    public Filters(PriceFilter priceFilter, LotSizeFilter lotSizeFilter, NotionalFilter notionalFilter)
    {
        PriceFilter = priceFilter;
        LotSizeFilter = lotSizeFilter;
        NotionalFilter = notionalFilter;
    }

    public PriceFilter PriceFilter { get; }
    public LotSizeFilter LotSizeFilter { get; }
    public NotionalFilter NotionalFilter { get; }
}

internal class PriceFilter : Filter
{
    public PriceFilter(string filterType, decimal tickSize, decimal minPrice, decimal maxPrice)
        : base(filterType)
    {
        TickSize = tickSize;
        MinPrice = minPrice;
        MaxPrice = maxPrice;
    }

    public decimal TickSize { get; }
    public decimal MinPrice { get; }
    public decimal MaxPrice { get; }
}

internal class LotSizeFilter : Filter
{
    public LotSizeFilter(string filterType, decimal stepSize, decimal minQty, decimal maxQty)
        : base(filterType)
    {
        StepSize = stepSize;
        MinQty = minQty;
        MaxQty = maxQty;
    }

    public decimal StepSize { get; }
    public decimal MinQty { get; }
    public decimal MaxQty { get; }
}

internal class NotionalFilter : Filter
{
    public NotionalFilter(string filterType, decimal minNotional, decimal maxNotional)
        : base(filterType)
    {
        MinNotional = minNotional;
        MaxNotional = maxNotional;
    }

    public decimal MinNotional { get; }
    public decimal MaxNotional { get; }
}

internal abstract class Filter
{
    protected Filter(string filterType)
    {
        FilterType = filterType;
    }

    public string FilterType { get; }
}
