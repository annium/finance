using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Domain.Market;
using Annium.Finance.Providers.Abstractions.Domain.Market.Operations;
using NodaTime;

namespace Annium.Finance.Providers.Abstractions.Connectors.Market;

/// <summary>
/// Low-level data access to a market data source: resolves resources/instruments and historical candles.
/// Consumed by an <see cref="IMarketConnector"/> implementation rather than directly by application code.
/// </summary>
public interface IMarketProvider
{
    /// <summary>
    /// Loads the market context (available resources and instruments) from the provider.
    /// </summary>
    /// <returns>A result carrying the market context on success, or null data with a non-success status on failure.</returns>
    Task<MarketResult<MarketContext?>> LoadContextAsync();

    /// <summary>
    /// Loads historical candles for an instrument over the given time range, yielding successive batches of
    /// consecutive one-minute candles as they are fetched from the provider. Enumeration ends once the range
    /// has been covered or a fetch returns no more data; a failed fetch is yielded as a final non-success
    /// batch, so a caller can tell a truncated range from a complete one.
    /// </summary>
    /// <param name="instrument">The instrument symbol to load candles for.</param>
    /// <param name="start">The inclusive start of the time range.</param>
    /// <param name="end">The exclusive end of the time range.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>An async sequence of candle batch results covering the requested range.</returns>
    IAsyncEnumerable<MarketResult<IReadOnlyCollection<CandleModel>?>> LoadCandlesAsync(
        string instrument,
        Instant start,
        Instant end,
        CancellationToken ct
    );
}
