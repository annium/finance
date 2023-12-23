using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Connectors.Connectors;
using Annium.Finance.Providers.Abstractions.Domain.Dto;
using Annium.Finance.Providers.Abstractions.Domain.Models;
using Annium.Finance.Providers.Abstractions.Domain.Operations;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Connectors;

internal class UserProvider : IUserProvider
{
    public Task<UserResult<IReadOnlyCollection<OrderDto>>> LoadOrdersAsync(
        UserSettings config,
        IReadOnlyCollection<string> instruments,
        long? since
    )
    {
        throw new NotImplementedException();
    }

    public Task<UserResult<IReadOnlyCollection<TradeDto>>> LoadTradesAsync(
        UserSettings config,
        IReadOnlyCollection<string> instruments,
        long? since
    )
    {
        throw new NotImplementedException();
    }
}
