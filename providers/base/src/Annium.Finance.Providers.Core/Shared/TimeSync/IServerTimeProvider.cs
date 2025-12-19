using System.Threading;
using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Domain.Market.Operations;

namespace Annium.Finance.Providers.Core.Shared.TimeSync;

public interface IServerTimeProvider
{
    Task<MarketResult<long>> LoadAsync(CancellationToken ct);
}
