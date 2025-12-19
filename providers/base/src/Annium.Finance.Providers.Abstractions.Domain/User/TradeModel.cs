namespace Annium.Finance.Providers.Abstractions.Domain.User;

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
