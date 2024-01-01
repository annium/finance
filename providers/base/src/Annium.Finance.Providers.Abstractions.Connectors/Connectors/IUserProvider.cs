using System.Collections.Generic;
using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Domain.Models;
using Annium.Finance.Providers.Abstractions.Domain.Operations;

namespace Annium.Finance.Providers.Abstractions.Connectors.Connectors;

public interface IUserProvider
{
    Task<UserResult<IReadOnlyCollection<OrderModel>>> LoadOrdersAsync(
        UserSettings config,
        IReadOnlyCollection<string> instruments,
        long? since
    );

    Task<UserResult<IReadOnlyCollection<TradeModel>>> LoadTradesAsync(
        UserSettings config,
        IReadOnlyCollection<string> instruments,
        long? since
    );
}
