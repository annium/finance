namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.User.Contracts.Domain;

/// <summary>
/// The user data stream <c>ACCOUNT_CONFIG_UPDATE</c> event, raised when the account's multi-assets margin mode
/// or a symbol's leverage changes. <see cref="Symbol"/> and <see cref="Leverage"/> are only meaningful when
/// <see cref="Type"/> is <see cref="AccountConfigUpdateEventType.LeverageChange"/>; <see cref="MultiAssetsMode"/>
/// is only meaningful when it is <see cref="AccountConfigUpdateEventType.MultiAssetsModeChange"/>.
/// </summary>
/// <param name="Date">The event timestamp, in Unix milliseconds.</param>
/// <param name="Type">Which part of the account configuration changed.</param>
/// <param name="MultiAssetsMode">The multi-assets margin mode after the change.</param>
/// <param name="Symbol">The symbol whose leverage changed.</param>
/// <param name="Leverage">The leverage set for <see cref="Symbol"/> after the change.</param>
internal sealed record AccountConfigUpdateEvent(
    long Date,
    AccountConfigUpdateEventType Type,
    bool MultiAssetsMode,
    string Symbol,
    int Leverage
);

/// <summary>
/// Which part of the account configuration an <see cref="AccountConfigUpdateEvent"/> reports a change to.
/// </summary>
internal enum AccountConfigUpdateEventType
{
    /// <summary>The account's multi-assets margin mode changed.</summary>
    MultiAssetsModeChange,

    /// <summary>A symbol's leverage changed.</summary>
    LeverageChange,
}
