using System.Collections.Generic;
using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Domain.Dto;
using Annium.Finance.Providers.Abstractions.Domain.Models;
using Annium.Finance.Providers.Abstractions.Domain.Operations;
using NodaTime;

namespace Annium.Finance.Providers.Abstractions.Connectors.Connectors;

public interface IUserProvider
{
    Task<UserResult<IReadOnlyCollection<OrderDto>>> LoadOrdersAsync(
        UserSettings config,
        IReadOnlyCollection<string> instruments,
        Instant? since
    );
}
