using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Connectors.Shared;
using Annium.Finance.Providers.Abstractions.Domain.Market;

namespace Annium.Finance.Providers.Abstractions.Connectors.Market;

/// <summary>
/// Live connection to a market data source. Exposes the resources and instruments known to the underlying
/// provider, streams instrument tickers, and lets callers manage which symbols are streamed.
/// </summary>
public interface IMarketConnector : IConnectorBase
{
    /// <summary>Gets the resources (assets) currently known to the connector, as loaded on the last sync.</summary>
    IReadOnlyCollection<ResourceModel> Resources { get; }

    /// <summary>Gets the instruments currently known to the connector, as loaded on the last sync.</summary>
    IReadOnlyCollection<InstrumentModel> Instruments { get; }

    /// <summary>
    /// An observable stream of instrument ticker updates. A ticker arrives whenever the provider pushes a price
    /// update for a symbol the connector is currently subscribed to.
    /// </summary>
    IObservable<InstrumentTicker> Tickers { get; }

    /// <summary>
    /// Raised during a sync cycle, once resources and instruments have been reloaded and before the connector
    /// resumes ticker subscriptions and reports itself as connected. Handlers can use this to re-subscribe to
    /// tickers for the refreshed instrument set; the connector waits for the handler to complete.
    /// </summary>
    event Func<MarketSettings, IReadOnlyCollection<ResourceModel>, IReadOnlyCollection<InstrumentModel>, Task> OnSync;

    /// <summary>
    /// Forces a resync: the connector reports itself as connecting, reloads resources and instruments, fires
    /// <see cref="OnSync"/>, and reports itself as connected again.
    /// </summary>
    void Sync();

    /// <summary>
    /// Subscribes to ticker updates for the given instrument symbols. Matching updates start arriving on
    /// <see cref="Tickers"/>.
    /// </summary>
    /// <param name="symbols">The instrument symbols to subscribe to.</param>
    void SubscribeTickers(IReadOnlyCollection<string> symbols);

    /// <summary>
    /// Unsubscribes from ticker updates for the given instrument symbols. Matching updates stop arriving on
    /// <see cref="Tickers"/>.
    /// </summary>
    /// <param name="symbols">The instrument symbols to unsubscribe from.</param>
    void UnsubscribeTickers(IReadOnlyCollection<string> symbols);
}
