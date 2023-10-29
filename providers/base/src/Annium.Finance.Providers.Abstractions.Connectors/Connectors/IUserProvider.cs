using System.Collections.Generic;
using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Domain.Dto;
using Annium.Finance.Providers.Abstractions.Domain.Interfaces;
using Annium.Finance.Providers.Abstractions.Domain.Operations;
using NodaTime;

namespace Annium.Finance.Providers.Abstractions.Connectors.Connectors;

public interface IUserProvider
{
    Task<UserResult<UserContext>> LoadContextAsync(
        IUserConfig config,
        IReadOnlyCollection<string> instruments,
        Instant? loadOrdersSince
    );
}

public sealed record UserContext(
    IReadOnlyCollection<AssetDto> Assets,
    IReadOnlyCollection<PositionDto> Positions,
    IReadOnlyCollection<OrderDto> Orders
);
