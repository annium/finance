using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Connectors.Connectors;
using Annium.Finance.Providers.Abstractions.Domain.Interfaces;
using Annium.Finance.Providers.Abstractions.Domain.Operations;
using NodaTime;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Connectors;

internal class UserProvider : IUserProvider
{
    public Task<UserResult<UserContext>> LoadContextAsync(
        IUserConfig config,
        IReadOnlyCollection<string> instruments,
        Instant? loadOrdersSince
    )
    {
        throw new NotImplementedException();
    }
}
