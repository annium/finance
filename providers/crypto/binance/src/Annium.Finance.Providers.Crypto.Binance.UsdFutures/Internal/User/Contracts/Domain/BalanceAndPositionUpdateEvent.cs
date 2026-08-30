using System.Collections.Generic;
using Annium.Finance.Providers.Abstractions.Domain.User;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.User.Contracts.Domain;

/// <summary>
/// The user data stream <c>ACCOUNT_UPDATE</c> event, raised whenever a trade, funding settlement, deposit,
/// withdrawal, liquidation or ADL changes account balances and/or open positions. Carries the full snapshot of
/// every affected balance and position, not just deltas.
/// </summary>
/// <param name="Date">The event timestamp, in Unix milliseconds.</param>
/// <param name="Balances">The asset balances affected by the change.</param>
/// <param name="Positions">The positions affected by the change.</param>
internal sealed record BalanceAndPositionUpdateEvent(
    long Date,
    IReadOnlyCollection<BalanceAndPositionUpdateEventBalance> Balances,
    IReadOnlyCollection<BalanceAndPositionUpdateEventPosition> Positions
);

/// <summary>
/// The updated balance of a single asset within a <see cref="BalanceAndPositionUpdateEvent"/>.
/// </summary>
/// <param name="Asset">The asset code.</param>
/// <param name="WalletBalance">The asset's wallet balance after the change.</param>
/// <param name="CrossWalletBalance">The asset's cross-margin wallet balance after the change.</param>
/// <param name="BalanceChange">The change in wallet balance excluding unrealized PnL (e.g. from a deposit/withdrawal).</param>
internal sealed record BalanceAndPositionUpdateEventBalance(
    string Asset,
    decimal WalletBalance,
    decimal CrossWalletBalance,
    decimal BalanceChange
);

/// <summary>
/// The updated state of a single position within a <see cref="BalanceAndPositionUpdateEvent"/>.
/// </summary>
/// <param name="Symbol">The instrument symbol.</param>
/// <param name="Orientation">The position side (long/short in hedge mode, or both in one-way mode).</param>
/// <param name="MarginType">Whether the position uses cross or isolated margin.</param>
/// <param name="IsolatedWallet">The isolated margin wallet balance, when <see cref="MarginType"/> is isolated.</param>
/// <param name="Amount">The signed position amount (positive for long, negative for short).</param>
/// <param name="AveragePrice">The position's average entry price.</param>
/// <param name="UnrealizedPnl">The position's unrealized profit or loss.</param>
internal sealed record BalanceAndPositionUpdateEventPosition(
    string Symbol,
    OrientationRange Orientation,
    MarginType MarginType,
    decimal IsolatedWallet,
    decimal Amount,
    decimal AveragePrice,
    decimal UnrealizedPnl
);
