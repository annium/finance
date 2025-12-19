using System.Collections.Generic;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Internal.User.Contracts.Domain;

internal sealed record AccountUpdateEvent(long Date, IReadOnlyCollection<AccountUpdateEventBalance> Balances);

internal sealed record AccountUpdateEventBalance(string Asset, decimal Free, decimal Locked);
