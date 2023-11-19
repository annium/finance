using System.Threading.Tasks;
using Annium.Data.Tables;
using Annium.Finance.Providers.Abstractions.Connectors.Connectors;
using Annium.Finance.Providers.Abstractions.Domain.Dto;
using Annium.Finance.Providers.Abstractions.Domain.Interfaces;

namespace Annium.Finance.Providers.Abstractions.Connectors.Sync;

public interface IUserSynchronizer
{
    Task ExecuteAsync(
        IUserConfig config,
        IUserProvider provider,
        ITable<AssetDto> assets,
        ITable<PositionDto> positions,
        ITable<OrderDto> orders
    );
}
