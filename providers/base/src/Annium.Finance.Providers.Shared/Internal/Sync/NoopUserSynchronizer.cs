using System.Threading.Tasks;
using Annium.Data.Tables;
using Annium.Finance.Providers.Abstractions.Connectors.Connectors;
using Annium.Finance.Providers.Abstractions.Connectors.Sync;
using Annium.Finance.Providers.Abstractions.Domain.Dto;
using Annium.Finance.Providers.Abstractions.Domain.Interfaces;

namespace Annium.Finance.Providers.Shared.Internal.Sync;

internal class NoopUserSynchronizer : IUserSynchronizer
{
    public Task ExecuteAsync(
        IUserConfig config,
        IUserProvider provider,
        ITable<AssetDto> assets,
        ITable<PositionDto> positions,
        ITable<OrderDto> orders
    )
    {
        return Task.CompletedTask;
    }
}
