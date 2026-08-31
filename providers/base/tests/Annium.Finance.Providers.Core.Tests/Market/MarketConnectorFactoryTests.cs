using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Annium.Core.DependencyInjection;
using Annium.Finance.Providers.Abstractions.Connectors.Market;
using Annium.Finance.Providers.Abstractions.Connectors.Shared;
using Annium.Finance.Providers.Abstractions.Domain.Market;
using Annium.Finance.Providers.Abstractions.Domain.Shared;
using Annium.Finance.Providers.Tests.Lib;
using Annium.Testing;
using Xunit;

namespace Annium.Finance.Providers.Core.Tests.Market;

/// <summary>
/// Pins what the market connector factory hands back. Until now nothing exercised this path offline at all:
/// its only callers are the connector test bases, and every test built on those talks to the live exchange
/// and is skipped — so any change here, including dropping the scope pairing entirely, went unnoticed.
/// </summary>
public class MarketConnectorFactoryTests : ProvidersTestBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MarketConnectorFactoryTests"/> class, registering a
    /// stand-in instance factory so the real factory has a provider to build for.
    /// </summary>
    /// <param name="outputHelper">The xUnit output helper used to capture test logs.</param>
    public MarketConnectorFactoryTests(ITestOutputHelper outputHelper)
        : base(outputHelper)
    {
        Register(container =>
        {
            container.Add<ScopeCapture>().AsSelf().Singleton();
            container.Add<FakeInstanceFactory>().AsKeyed<IMarketConnectorInstanceFactory>("fake").Scoped();
        });
    }

    /// <summary>
    /// A connector built through the factory carries the scope it was resolved from, and disposing the
    /// connector disposes that scope. The factory creates a scope per connector and resolves everything the
    /// connector needs from it, so nothing else is in a position to release it.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task CreatedConnector_DisposesTheScopeItWasBuiltFrom()
    {
        // arrange
        var capture = Get<ScopeCapture>();
        var factory = Get<IMarketConnectorFactory>();
        var settings = new MarketSettings { Provider = "fake", Environment = ProviderEnvironment.Test };

        // act
        var connector = factory.Create(settings);

        // assert - the connector was built inside a live scope
        var scope = capture.Scope.NotNull();
        IsAlive(scope).IsTrue();

        // act
        await connector.DisposeAsync();

        // assert - and disposing it took that scope with it
        IsAlive(scope).IsFalse("a connector built by the factory must carry its scope's lifetime");
    }

    /// <summary>
    /// Reports whether a service provider still serves resolutions, which is how disposal is observed.
    /// </summary>
    /// <param name="sp">The provider to probe.</param>
    /// <returns><see langword="true"/> while it can still resolve; otherwise <see langword="false"/>.</returns>
    private static bool IsAlive(IServiceProvider sp)
    {
        try
        {
            sp.GetService(typeof(IServiceProvider));

            return true;
        }
        catch (ObjectDisposedException)
        {
            return false;
        }
    }

    /// <summary>Carries the scoped provider a connector was built from out to the test.</summary>
    private sealed class ScopeCapture
    {
        /// <summary>Gets or sets the scoped provider the instance factory was resolved from.</summary>
        public IServiceProvider? Scope { get; set; }
    }

    /// <summary>
    /// Stands in for a provider's own connector factory, recording the scope it was resolved from.
    /// </summary>
    /// <param name="sp">The scoped provider this factory was resolved from.</param>
    /// <param name="capture">The carrier the scope is reported through.</param>
    private sealed class FakeInstanceFactory(IServiceProvider sp, ScopeCapture capture)
        : IMarketConnectorInstanceFactory
    {
        /// <summary>
        /// Records the scope and returns a connector that owns nothing.
        /// </summary>
        /// <param name="settings">Ignored.</param>
        /// <param name="disposable">Ignored.</param>
        /// <returns>A connector that does nothing.</returns>
        public IMarketConnector Create(MarketSettings settings, AsyncDisposableBox disposable)
        {
            capture.Scope = sp;

            return new StubConnector();
        }
    }

    /// <summary>A connector that does nothing, so the test observes only the factory's own behaviour.</summary>
    private sealed class StubConnector : IMarketConnector
    {
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

        /// <summary>Does nothing.</summary>
        /// <returns>A completed task.</returns>
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
