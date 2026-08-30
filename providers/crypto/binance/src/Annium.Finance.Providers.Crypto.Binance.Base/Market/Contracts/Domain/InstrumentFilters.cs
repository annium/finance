namespace Annium.Finance.Providers.Crypto.Binance.Base.Market.Contracts.Domain;

/// <summary>The set of Binance instrument trading filters (<c>LOT_SIZE</c>, <c>PRICE_FILTER</c>, notional and max-orders) that constrain orders placed on a symbol.</summary>
/// <param name="LotSizeFilter">The quantity constraints (<c>LOT_SIZE</c>).</param>
/// <param name="PriceFilter">The price constraints (<c>PRICE_FILTER</c>).</param>
/// <param name="NotionalFilter">The notional value constraints.</param>
/// <param name="MaxOrdersFilter">The maximum number of open orders allowed for the symbol.</param>
public sealed record InstrumentFilters(
    LotSizeFilter LotSizeFilter,
    PriceFilter PriceFilter,
    NotionalFilter NotionalFilter,
    MaxOrdersFilter MaxOrdersFilter
);

/// <summary>Binance's <c>LOT_SIZE</c> filter, constraining the order quantity for a symbol.</summary>
/// <param name="MinQty">The minimum order quantity.</param>
/// <param name="MaxQty">The maximum order quantity.</param>
/// <param name="StepSize">The increment the order quantity must be a multiple of.</param>
public sealed record LotSizeFilter(decimal MinQty, decimal MaxQty, decimal StepSize);

/// <summary>Binance's <c>PRICE_FILTER</c>, constraining the order price for a symbol.</summary>
/// <param name="MinPrice">The minimum order price.</param>
/// <param name="MaxPrice">The maximum order price.</param>
/// <param name="TickSize">The increment the order price must be a multiple of.</param>
public sealed record PriceFilter(decimal MinPrice, decimal MaxPrice, decimal TickSize);

/// <summary>Binance's notional value filter, constraining the minimum and maximum order value for a symbol.</summary>
/// <param name="MinNotional">The minimum order notional value (price times quantity).</param>
/// <param name="MaxNotional">The maximum order notional value (price times quantity).</param>
public sealed record NotionalFilter(decimal MinNotional, decimal MaxNotional);

/// <summary>Binance's filter on the maximum number of open orders allowed for a symbol.</summary>
/// <param name="MaxOrders">The maximum number of open orders allowed.</param>
public sealed record MaxOrdersFilter(int MaxOrders);
