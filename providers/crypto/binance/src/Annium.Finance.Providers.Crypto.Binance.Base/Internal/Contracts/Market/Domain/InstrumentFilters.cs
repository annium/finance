namespace Annium.Finance.Providers.Crypto.Binance.Base.Internal.Contracts.Market.Domain;

internal sealed record InstrumentFilters(
    LotSizeFilter LotSizeFilter,
    PriceFilter PriceFilter,
    NotionalFilter NotionalFilter,
    MaxOrdersFilter MaxOrdersFilter
);

internal sealed record LotSizeFilter(decimal MinQty, decimal MaxQty, decimal StepSize);

internal sealed record PriceFilter(decimal MinPrice, decimal MaxPrice, decimal TickSize);

internal sealed record NotionalFilter(decimal MinNotional, decimal MaxNotional);

internal sealed record MaxOrdersFilter(int MaxOrders);
