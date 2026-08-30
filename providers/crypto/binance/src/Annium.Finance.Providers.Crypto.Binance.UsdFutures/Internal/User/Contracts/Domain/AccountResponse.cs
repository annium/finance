using System.Collections.Generic;
using Annium.Finance.Providers.Abstractions.Domain.User;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.User.Contracts.Domain;

/// <summary>
/// The parsed response of the <c>GET /fapi/v2/account</c> endpoint: a full snapshot of the account's asset
/// balances and open positions.
/// </summary>
/// <param name="Balances">The account's asset balances.</param>
/// <param name="Positions">The account's open positions.</param>
internal sealed record AccountResponse(
    IReadOnlyCollection<AccountResponseBalance> Balances,
    IReadOnlyCollection<AccountResponsePosition> Positions
);

/// <summary>
/// The balance of a single asset within an <see cref="AccountResponse"/>.
/// </summary>
/// <param name="Asset">The asset code.</param>
/// <param name="Total">The margin balance (wallet balance plus unrealized PnL).</param>
/// <param name="Free">The maximum amount currently available to withdraw.</param>
/// <param name="InitialMargin">The margin currently required to keep open positions and orders.</param>
/// <param name="MaintenanceMargin">The minimum margin required to avoid liquidation.</param>
/// <param name="UpdatedDate">The timestamp of the last account update, in Unix milliseconds.</param>
internal sealed record AccountResponseBalance(
    string Asset,
    decimal Total,
    decimal Free,
    decimal InitialMargin,
    decimal MaintenanceMargin,
    long UpdatedDate
);

/// <summary>
/// A single position within an <see cref="AccountResponse"/>.
/// </summary>
/// <param name="Symbol">The instrument symbol.</param>
/// <param name="Orientation">The position side (long/short in hedge mode, or both in one-way mode).</param>
/// <param name="MarginType">Whether the position uses cross or isolated margin.</param>
/// <param name="Leverage">The leverage currently set for the position.</param>
/// <param name="Amount">The signed position amount (positive for long, negative for short).</param>
/// <param name="AveragePrice">The position's average entry price.</param>
/// <param name="UnrealizedPnl">The position's unrealized profit or loss.</param>
/// <param name="UpdatedDate">The timestamp of the last position update, in Unix milliseconds.</param>
internal sealed record AccountResponsePosition(
    string Symbol,
    OrientationRange Orientation,
    MarginType MarginType,
    decimal Leverage,
    decimal Amount,
    decimal AveragePrice,
    decimal UnrealizedPnl,
    long UpdatedDate
);
