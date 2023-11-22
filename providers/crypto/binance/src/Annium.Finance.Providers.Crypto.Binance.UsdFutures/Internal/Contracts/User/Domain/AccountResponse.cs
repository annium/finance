using System.Collections.Generic;
using Annium.Finance.Providers.Abstractions.Domain.Enums;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Contracts.User.Domain;

public readonly record struct AccountResponse(
    IReadOnlyCollection<AccountResponseBalance> Balances,
    IReadOnlyCollection<AccountResponsePosition> Positions
);

public readonly record struct AccountResponseBalance(
    string Asset,
    decimal Total,
    decimal Free,
    decimal InitialMargin,
    decimal MaintenanceMargin,
    long UpdatedDate
);

public readonly record struct AccountResponsePosition(
    string Symbol,
    OrientationRange Orientation,
    MarginType MarginType,
    int Leverage,
    decimal Amount,
    decimal AveragePrice,
    decimal UnrealizedPnl,
    long UpdatedDate
);
