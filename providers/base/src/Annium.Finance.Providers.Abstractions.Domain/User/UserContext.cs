using System.Collections.Generic;

namespace Annium.Finance.Providers.Abstractions.Domain.User;

/// <summary>
/// Represents a snapshot of an account's full trading state: its balances and open positions.
/// </summary>
/// <param name="Assets">The account's balances across all resources.</param>
/// <param name="Positions">The account's open positions across all instruments.</param>
public sealed record UserContext(IReadOnlyCollection<AssetModel> Assets, IReadOnlyCollection<PositionModel> Positions);
