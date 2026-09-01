using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Connectors.Market;
using Annium.Finance.Providers.Abstractions.Connectors.Shared;
using Annium.Finance.Providers.Abstractions.Domain.Market;
using Microsoft.Extensions.DependencyInjection;

namespace Annium.Finance.Providers.Core.Internal.Market;

/// <summary>
/// A market connector together with the DI scope it was built from, so the scope outlives it.
/// </summary>
/// <remarks>
/// The connector's own resources are resolved from this scope — its provider above all, which its
/// <c>OnSync</c> contract hands to handlers by design. Registering the scope in the same disposable box as
/// the connector's executor left the two as unordered siblings: that box drains its asynchronous entries
/// concurrently, so tearing the scope down could overtake the executor still draining a sync cycle that was
/// using what the scope owns. Disposing the connector first and the scope after is the ordering the
/// dependency actually has.
/// </remarks>
/// <param name="inner">The connector this wraps.</param>
/// <param name="scope">The DI scope the connector was built from.</param>
internal sealed class ScopedMarketConnector(IMarketConnector inner, AsyncServiceScope scope) : IMarketConnector
{
    /// <summary>Gets the current connection status of the connector.</summary>
    public ConnectorStatus Status => inner.Status;

    /// <summary>Gets the resources (assets) currently known to the connector, as loaded on the last sync.</summary>
    public IReadOnlyCollection<ResourceModel> Resources => inner.Resources;

    /// <summary>Gets the instruments currently known to the connector, as loaded on the last sync.</summary>
    public IReadOnlyCollection<InstrumentModel> Instruments => inner.Instruments;

    /// <summary>An observable stream of instrument ticker updates.</summary>
    public IObservable<InstrumentTicker> Tickers => inner.Tickers;

    /// <summary>Raised whenever <see cref="Status"/> changes, with the new status.</summary>
    public event Action<ConnectorStatus> OnStatusChanged
    {
        add => inner.OnStatusChanged += value;
        remove => inner.OnStatusChanged -= value;
    }

    /// <summary>Raised when the connector encounters an error.</summary>
    public event Action<ConnectorError> OnError
    {
        add => inner.OnError += value;
        remove => inner.OnError -= value;
    }

    /// <summary>Raised during a sync cycle, once resources and instruments have been reloaded.</summary>
    public event Func<
        MarketSettings,
        IReadOnlyCollection<ResourceModel>,
        IReadOnlyCollection<InstrumentModel>,
        Task
    > OnSync
    {
        add => inner.OnSync += value;
        remove => inner.OnSync -= value;
    }

    /// <summary>Forces a resync.</summary>
    public void Sync() => inner.Sync();

    /// <summary>Subscribes to ticker updates for the given instrument symbols.</summary>
    /// <param name="symbols">The instrument symbols to subscribe to.</param>
    public void SubscribeTickers(IReadOnlyCollection<string> symbols) => inner.SubscribeTickers(symbols);

    /// <summary>Unsubscribes from ticker updates for the given instrument symbols.</summary>
    /// <param name="symbols">The instrument symbols to unsubscribe from.</param>
    public void UnsubscribeTickers(IReadOnlyCollection<string> symbols) => inner.UnsubscribeTickers(symbols);

    /// <summary>
    /// Disposes the connector, then the scope it was built from.
    /// </summary>
    /// <returns>A task that completes once both have been disposed.</returns>
    public async ValueTask DisposeAsync()
    {
        await inner.DisposeAsync();
        await scope.DisposeAsync();
    }
}
