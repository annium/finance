using System.Collections.Generic;
using Annium.Finance.Providers.Abstractions.Domain.Enums;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Contracts.User.Domain;

public readonly record struct BalanceAndPositionUpdateEvent(
    long Date,
    IReadOnlyCollection<BalanceAndPositionUpdateEventBalance> Balances,
    IReadOnlyCollection<BalanceAndPositionUpdateEventPosition> Positions
);

public readonly record struct BalanceAndPositionUpdateEventBalance(
    string Asset,
    decimal WalletBalance,
    decimal CrossWalletBalance,
    decimal BalanceChange
);

public readonly record struct BalanceAndPositionUpdateEventPosition(
    string Symbol,
    OrientationRange Orientation,
    MarginType MarginType,
    decimal IsolatedWallet,
    decimal Amount,
    decimal AveragePrice,
    decimal UnrealizedPnl
);
