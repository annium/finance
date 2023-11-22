using System.Collections.Generic;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Internal.Contracts.User.Domain;

internal readonly record struct AccountUpdateEvent(long Date, IReadOnlyCollection<AccountUpdateEventBalance> Balances);

internal readonly record struct AccountUpdateEventBalance(string Asset, decimal Free, decimal Locked);
