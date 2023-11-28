namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Contracts.User.Domain;

internal sealed record AccountConfigurationUpdateEvent(
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
