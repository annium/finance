namespace Annium.Finance.Providers.Crypto.Binance.Spot.Internal.Contracts.User.Domain;

internal sealed record TradeResponse(
    string OrderId,
    string Symbol,
    decimal Price,
    decimal Qty,
    string CommissionAsset,
    decimal Commission,
    bool Maker,
    long Moment
);
