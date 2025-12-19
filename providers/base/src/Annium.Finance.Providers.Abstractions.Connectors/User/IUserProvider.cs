using System.Collections.Generic;
using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Domain.User;
using Annium.Finance.Providers.Abstractions.Domain.User.Operations;

namespace Annium.Finance.Providers.Abstractions.Connectors.User;

public interface IUserProvider
{
    Task<UserResult<UserContext?>> LoadContextAsync(UserSettings settings);

    Task<UserResult<IReadOnlyCollection<OrderModel>?>> LoadOpenOrdersAsync(UserSettings settings);

    Task<UserResult<IReadOnlyCollection<OrderModel>?>> LoadOrdersAsync(
        UserSettings settings,
        string symbol,
        long? since
    );

    Task<UserResult<IReadOnlyCollection<TradeModel>?>> LoadTradesAsync(
        UserSettings settings,
        string symbol,
        long? since
    );
}
