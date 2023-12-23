namespace Annium.Finance.Providers.Abstractions.Domain.Dto;

public sealed record TradeDto(
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
