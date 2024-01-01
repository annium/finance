using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Connectors.Connectors;
using Annium.Finance.Providers.Abstractions.Domain.Models;
using Annium.Finance.Providers.Abstractions.Domain.Operations;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Connectors;

internal class UserProvider : IUserProvider
{
    public Task<UserResult<IReadOnlyCollection<OrderModel>>> LoadOrdersAsync(
        UserSettings settings,
        IReadOnlyCollection<string> instruments,
        long? since
    )
    {
        throw new NotImplementedException();
    }

    public Task<UserResult<IReadOnlyCollection<TradeModel>>> LoadTradesAsync(
        UserSettings settings,
        IReadOnlyCollection<string> instruments,
        long? since
    )
    {
        throw new NotImplementedException();
    }
}
