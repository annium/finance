namespace Annium.Finance.Providers.Abstractions.Domain.Dto;

public sealed record TradeDto(
    string OrderId,
    string Symbol,
    decimal Price,
    decimal Qty,
    string CommissionAsset,
    decimal Commission,
    bool Maker,
    long Moment
);
