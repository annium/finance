namespace Annium.Finance.Providers.Abstractions.Domain.Market;

/// <summary>
/// Describes a tradable instrument: its resources and the quantity/price constraints and limits orders must satisfy.
/// </summary>
/// <param name="Symbol">The instrument's trading symbol, as used by the provider (e.g. "BTCUSDT").</param>
/// <param name="Target">The base resource being traded (the asset whose quantity an order buys or sells).</param>
/// <param name="Quote">The resource an order's price is denominated in.</param>
/// <param name="Currency">The resource notional values and fees are settled in.</param>
/// <param name="MinQty">The minimum order quantity, in the target resource.</param>
/// <param name="MaxQty">The maximum order quantity, in the target resource.</param>
/// <param name="LotSize">The quantity step orders must be a multiple of, in the target resource.</param>
/// <param name="MinPrice">The minimum allowed order price.</param>
/// <param name="MaxPrice">The maximum allowed order price.</param>
/// <param name="TickSize">The price step order prices must be a multiple of.</param>
/// <param name="MinSum">The minimum order notional value (quantity multiplied by price).</param>
/// <param name="MaxSum">The maximum order notional value (quantity multiplied by price).</param>
/// <param name="MaxOrders">The maximum number of open orders allowed on this instrument at once.</param>
public sealed record InstrumentModel(
    string Symbol,
    ResourceModel Target,
    ResourceModel Quote,
    ResourceModel Currency,
    decimal MinQty,
    decimal MaxQty,
    decimal LotSize,
    decimal MinPrice,
    decimal MaxPrice,
    decimal TickSize,
    decimal MinSum,
    decimal MaxSum,
    int MaxOrders
) : IInstrument
{
    /// <summary>Returns the instrument's trading symbol.</summary>
    /// <returns>The value of <see cref="Symbol"/>.</returns>
    public override string ToString() => Symbol;
}
