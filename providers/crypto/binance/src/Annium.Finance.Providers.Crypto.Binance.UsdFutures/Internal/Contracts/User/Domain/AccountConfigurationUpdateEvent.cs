namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Contracts.User.Domain;

public readonly record struct AccountConfigurationUpdateEvent(
    long Date,
    AccountConfigurationUpdateEventType Type,
    bool MultiAssetsMode,
    string Symbol,
    int Leverage
);

public enum AccountConfigurationUpdateEventType
{
    MultiAssetsModeChange,
    LeverageChange
}
