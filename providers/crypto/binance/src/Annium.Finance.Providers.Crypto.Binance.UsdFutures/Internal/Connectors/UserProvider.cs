using System.Collections.Generic;
using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Connectors.Connectors;
using Annium.Finance.Providers.Abstractions.Domain.Dto;
using Annium.Finance.Providers.Abstractions.Domain.Interfaces;
using Annium.Finance.Providers.Abstractions.Domain.Operations;
using NodaTime;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Connectors;

internal class UserProvider : IUserProvider
{
    public Task<
        Result<
            UserOperationStatus,
            (IReadOnlyCollection<AssetDto>, IReadOnlyCollection<PositionDto>, IReadOnlyCollection<OrderDto>)
        >
    > LoadAssetsAndPositionsAsync(IUserConfig config, IReadOnlyCollection<string> instruments, Instant? loadOrdersSince)
    {
        throw new System.NotImplementedException();
    }
}
