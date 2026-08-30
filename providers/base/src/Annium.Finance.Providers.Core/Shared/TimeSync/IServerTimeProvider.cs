using System.Threading;
using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Domain.Market.Operations;

namespace Annium.Finance.Providers.Core.Shared.TimeSync;

/// <summary>
/// Low-level access to a provider's server time endpoint. Consumed by an
/// <see cref="Annium.Finance.Providers.Core.Internal.Shared.TimeSync.ServerTimeSource"/> rather than directly by
/// application code.
/// </summary>
public interface IServerTimeProvider
{
    /// <summary>
    /// Loads the provider's current server time.
    /// </summary>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A result containing the server time as Unix milliseconds.</returns>
    Task<MarketResult<long>> LoadAsync(CancellationToken ct);
}
