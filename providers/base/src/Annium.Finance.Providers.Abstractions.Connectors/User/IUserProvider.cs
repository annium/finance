using System.Collections.Generic;
using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Domain.User;
using Annium.Finance.Providers.Abstractions.Domain.User.Operations;

namespace Annium.Finance.Providers.Abstractions.Connectors.User;

public interface IUserProvider
{
    Task<UserResult<UserContext?>> LoadContextAsync();
    Task<UserResult<IReadOnlyCollection<OrderModel>?>> LoadOpenOrdersAsync();
    Task<UserResult<IReadOnlyCollection<OrderModel>?>> LoadOrdersAsync(string symbol, long? since);
    Task<UserResult<IReadOnlyCollection<TradeModel>?>> LoadTradesAsync(string symbol, long? since);
}
