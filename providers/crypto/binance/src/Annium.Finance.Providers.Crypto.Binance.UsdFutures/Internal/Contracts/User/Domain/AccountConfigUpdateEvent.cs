namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Contracts.User.Domain;

internal sealed record AccountConfigUpdateEvent(
    long Date,
    AccountConfigUpdateEventType Type,
    bool MultiAssetsMode,
    string Symbol,
    int Leverage
);

internal enum AccountConfigUpdateEventType
{
    MultiAssetsModeChange,
    LeverageChange
}
