using System.Collections.Generic;
using Annium.Finance.Providers.Abstractions.Domain.Enums;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Contracts.User.Domain;

internal sealed record BalanceAndPositionUpdateEvent(
    long Date,
    IReadOnlyCollection<BalanceAndPositionUpdateEventBalance> Balances,
    IReadOnlyCollection<BalanceAndPositionUpdateEventPosition> Positions
);

internal sealed record BalanceAndPositionUpdateEventBalance(
    string Asset,
    decimal WalletBalance,
    decimal CrossWalletBalance,
    decimal BalanceChange
);

internal sealed record BalanceAndPositionUpdateEventPosition(
    string Symbol,
    OrientationRange Orientation,
    MarginType MarginType,
    decimal IsolatedWallet,
    decimal Amount,
    decimal AveragePrice,
    decimal UnrealizedPnl
);
