using System.Collections.Generic;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Internal.User.Contracts.Domain;

/// <summary>
/// A Binance user data stream <c>outboundAccountPosition</c> event, reporting the full set of asset balances
/// affected by an account change.
/// </summary>
/// <param name="Date">The Unix timestamp, in milliseconds, at which the balances were last updated.</param>
/// <param name="Balances">The affected asset balances.</param>
internal sealed record AccountUpdateEvent(long Date, IReadOnlyCollection<AccountUpdateEventBalance> Balances);

/// <summary>A single asset balance entry within an <see cref="AccountUpdateEvent"/>.</summary>
/// <param name="Asset">The asset code the balance is denominated in.</param>
/// <param name="Free">The quantity available for trading or withdrawal.</param>
/// <param name="Locked">The quantity reserved by open orders.</param>
internal sealed record AccountUpdateEventBalance(string Asset, decimal Free, decimal Locked);
