namespace Annium.Finance.Providers.Crypto.Binance.Base.Market.Contracts.Domain;

public sealed record InstrumentFilters(
    LotSizeFilter LotSizeFilter,
    PriceFilter PriceFilter,
    NotionalFilter NotionalFilter,
    MaxOrdersFilter MaxOrdersFilter
);

public sealed record LotSizeFilter(decimal MinQty, decimal MaxQty, decimal StepSize);

public sealed record PriceFilter(decimal MinPrice, decimal MaxPrice, decimal TickSize);

public sealed record NotionalFilter(decimal MinNotional, decimal MaxNotional);

public sealed record MaxOrdersFilter(int MaxOrders);
