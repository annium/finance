namespace Annium.Finance.Providers.Abstractions.Domain.Models;

public sealed record TradeModel(
    string Id,
    string OrderId,
    string Symbol,
    decimal Price,
    decimal Qty,
    string CommissionAsset,
    decimal CommissionAmount,
    bool Maker,
    long Moment
);
