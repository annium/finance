using System.Collections.Generic;
using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Domain.Models;
using Annium.Finance.Providers.Abstractions.Domain.Operations;

namespace Annium.Finance.Providers.Abstractions.Connectors.Connectors;

public interface IUserProvider
{
    Task<UserResult<UserContext?>> LoadContextAsync(UserSettings settings);

    Task<UserResult<IReadOnlyCollection<OrderModel>?>> LoadOrdersAsync(
        UserSettings settings,
        IReadOnlyCollection<string> symbols,
        long? since
    );

    Task<UserResult<IReadOnlyCollection<TradeModel>?>> LoadTradesAsync(
        UserSettings settings,
        IReadOnlyCollection<string> symbols,
        long since
    );
}
