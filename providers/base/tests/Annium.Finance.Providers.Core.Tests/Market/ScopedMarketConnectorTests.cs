using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Connectors.Market;
using Annium.Finance.Providers.Abstractions.Connectors.Shared;
using Annium.Finance.Providers.Abstractions.Domain.Market;
using Annium.Finance.Providers.Core.Internal.Market;
using Annium.Finance.Providers.Tests.Lib;
using Annium.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Annium.Finance.Providers.Core.Tests.Market;

/// <summary>
/// Pins the lifetime relationship between a connector and the DI scope it was built from. The connector's
/// provider and every other service it uses are resolved from that scope, and its <c>OnSync</c> contract
/// hands the provider to handlers by design — so the scope has to outlive the connector, not merely be torn
/// down somewhere alongside it.
/// </summary>
public class ScopedMarketConnectorTests : ProvidersTestBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ScopedMarketConnectorTests"/> class.
    /// </summary>
    /// <param name="outputHelper">The xUnit output helper used to capture test logs.</param>
    public ScopedMarketConnectorTests(ITestOutputHelper outputHelper)
        : base(outputHelper) { }

    /// <summary>
    /// The connector is disposed while its scope is still usable, and the scope is disposed after. Putting
    /// both in one disposable box gave neither guarantee: that box drains its asynchronous entries
    /// concurrently, so the scope could tear down while the connector's executor was still draining a sync
    /// cycle using what the scope owns.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task Disposal_TearsDownTheConnectorWhileItsScopeIsStillAlive()
    {
        // arrange
        var scope = Get<IServiceProvider>().CreateAsyncScope();
        var inner = new RecordingConnector(scope);
        var connector = new ScopedMarketConnector(inner, scope);

        // act
        await connector.DisposeAsync();

        // assert - the connector went first, and found its scope still serving
        inner.WasDisposed.IsTrue();
        inner.ScopeWasAliveOnDisposal.IsTrue("the connector must be torn down before the scope it resolved from");

        // assert - and the scope went after, rather than being left behind now that it is out of the box
        ScopeIsAlive(scope).IsFalse("the scope must be disposed once the connector is gone, not leaked");
    }

    /// <summary>
    /// Reports whether a scope still serves resolutions, which is how disposal is observed from outside.
    /// </summary>
    /// <param name="scope">The scope to probe.</param>
    /// <returns><see langword="true"/> while the scope can still resolve; otherwise <see langword="false"/>.</returns>
    private static bool ScopeIsAlive(AsyncServiceScope scope)
    {
        try
        {
            scope.ServiceProvider.GetService(typeof(IServiceProvider));

            return true;
        }
        catch (ObjectDisposedException)
        {
            return false;
        }
    }

    /// <summary>
    /// A connector that records, at the moment it is disposed, whether its scope was still alive.
    /// </summary>
    /// <param name="scope">The scope to probe on disposal.</param>
    private sealed class RecordingConnector(AsyncServiceScope scope) : IMarketConnector
    {
        /// <summary>Gets a value indicating whether this connector was disposed.</summary>
        public bool WasDisposed { get; private set; }

        /// <summary>Gets a value indicating whether the scope still served resolutions when this was disposed.</summary>
        public bool ScopeWasAliveOnDisposal { get; private set; }

        /// <summary>Gets the connector's status; unused by this test.</summary>
        public ConnectorStatus Status => ConnectorStatus.Connected;

        /// <summary>Gets the connector's resources; unused by this test.</summary>
        public IReadOnlyCollection<ResourceModel> Resources => [];

        /// <summary>Gets the connector's instruments; unused by this test.</summary>
        public IReadOnlyCollection<InstrumentModel> Instruments => [];

        /// <summary>Gets the connector's ticker stream; unused by this test.</summary>
        public IObservable<InstrumentTicker> Tickers => System.Reactive.Linq.Observable.Empty<InstrumentTicker>();

        /// <summary>Raised on status change; unused by this test.</summary>
        public event Action<ConnectorStatus> OnStatusChanged = delegate { };

        /// <summary>Raised on error; unused by this test.</summary>
        public event Action<ConnectorError> OnError = delegate { };

        /// <summary>Raised on sync; unused by this test.</summary>
        public event Func<
            MarketSettings,
            IReadOnlyCollection<ResourceModel>,
            IReadOnlyCollection<InstrumentModel>,
            Task
        > OnSync = delegate
        {
            return Task.CompletedTask;
        };

        /// <summary>Does nothing; unused by this test.</summary>
        public void Sync() => OnStatusChanged(ConnectorStatus.Connected);

        /// <summary>Does nothing; unused by this test.</summary>
        /// <param name="symbols">Ignored.</param>
        public void SubscribeTickers(IReadOnlyCollection<string> symbols) { }

        /// <summary>Does nothing; unused by this test.</summary>
        /// <param name="symbols">Ignored.</param>
        public void UnsubscribeTickers(IReadOnlyCollection<string> symbols) => OnError(new ConnectorError("unused"));

        /// <summary>Records that disposal happened, and whether the scope was still alive for it.</summary>
        /// <returns>A completed task.</returns>
        public ValueTask DisposeAsync()
        {
            WasDisposed = true;
            ScopeWasAliveOnDisposal = ScopeIsAlive(scope);

            return ValueTask.CompletedTask;
        }
    }
}
