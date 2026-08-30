namespace Annium.Finance.Providers.Abstractions.Domain.User;

/// <summary>
/// Represents a single fill (execution) of an order.
/// </summary>
/// <param name="Id">The provider-assigned identifier of the trade.</param>
/// <param name="OrderId">The identifier of the order this trade fills.</param>
/// <param name="Symbol">The instrument symbol the trade was executed on.</param>
/// <param name="Qty">The quantity filled by this trade, in the instrument's base asset.</param>
/// <param name="Price">The price the trade was executed at.</param>
/// <param name="CommissionAsset">The resource the trading commission was charged in.</param>
/// <param name="CommissionAmount">The commission charged for this trade, in <see cref="CommissionAsset"/>.</param>
/// <param name="Maker">Whether this side of the trade added liquidity to the order book (maker) rather than taking existing liquidity (taker).</param>
/// <param name="Moment">The Unix timestamp, in milliseconds, at which the trade was executed.</param>
public sealed record TradeModel(
    string Id,
    string OrderId,
    string Symbol,
    decimal Qty,
    decimal Price,
    string CommissionAsset,
    decimal CommissionAmount,
    bool Maker,
    long Moment
);
