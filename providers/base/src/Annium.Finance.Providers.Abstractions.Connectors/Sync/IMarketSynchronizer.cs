using System.Threading.Tasks;
using Annium.Data.Tables;
using Annium.Finance.Providers.Abstractions.Domain.Dto;
using Annium.Finance.Providers.Abstractions.Domain.Interfaces;

namespace Annium.Finance.Providers.Abstractions.Connectors.Sync;

public interface IMarketSynchronizer
{
    Task ExecuteAsync(IMarketConfig config, ITable<ResourceDto> resources, ITable<InstrumentDto> instruments);
}
