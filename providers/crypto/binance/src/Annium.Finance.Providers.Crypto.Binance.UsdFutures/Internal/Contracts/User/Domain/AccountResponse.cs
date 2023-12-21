using System.Collections.Generic;
using Annium.Finance.Providers.Abstractions.Domain.Enums;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Contracts.User.Domain;

internal sealed record AccountResponse(
    IReadOnlyCollection<AccountResponseBalance> Balances,
    IReadOnlyCollection<AccountResponsePosition> Positions
);

internal sealed record AccountResponseBalance(
    string Asset,
    decimal Total,
    decimal Free,
    decimal InitialMargin,
    decimal MaintenanceMargin,
    long UpdatedDate
);

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
