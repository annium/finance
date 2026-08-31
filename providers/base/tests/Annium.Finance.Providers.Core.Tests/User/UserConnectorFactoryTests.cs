using System;
using System.Threading.Tasks;
using Annium.Core.DependencyInjection;
using Annium.Data.Tables;
using Annium.Finance.Providers.Abstractions.Connectors.Shared;
using Annium.Finance.Providers.Abstractions.Connectors.User;
using Annium.Finance.Providers.Abstractions.Domain.Shared;
using Annium.Finance.Providers.Abstractions.Domain.User;
using Annium.Finance.Providers.Abstractions.Domain.User.Operations;
using Annium.Finance.Providers.Abstractions.Domain.User.Requests;
using Annium.Finance.Providers.Tests.Lib;
using Annium.Testing;
using Xunit;

namespace Annium.Finance.Providers.Core.Tests.User;

/// <summary>
/// Pins what the user connector factory hands back — the other half of a pair whose market side had the
/// same blind spot: its only callers are the connector test bases, and every test built on those talks to
/// the live exchange and is skipped.
/// </summary>
public class UserConnectorFactoryTests : ProvidersTestBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserConnectorFactoryTests"/> class, registering a
    /// stand-in instance factory so the real factory has a provider to build for.
    /// </summary>
    /// <param name="outputHelper">The xUnit output helper used to capture test logs.</param>
    public UserConnectorFactoryTests(ITestOutputHelper outputHelper)
        : base(outputHelper)
    {
        Register(container =>
        {
            container.Add<ScopeCapture>().AsSelf().Singleton();
            container.Add<FakeInstanceFactory>().AsKeyed<IUserConnectorInstanceFactory>("fake").Scoped();
        });
    }

    /// <summary>
    /// A connector built through the factory carries the scope it was resolved from, and disposing the
    /// connector disposes that scope.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task CreatedConnector_DisposesTheScopeItWasBuiltFrom()
    {
        // arrange
        var capture = Get<ScopeCapture>();
        var factory = Get<IUserConnectorFactory>();
        var settings = new UserSettings
        {
            Provider = "fake",
            Environment = ProviderEnvironment.Test,
            Key = "some_key",
            Secret = "some_secret",
        };

        // act
        var connector = factory.Create(settings);

        // assert
        var scope = capture.Scope.NotNull();
        IsAlive(scope).IsTrue();

        // act
        await connector.DisposeAsync();

        // assert
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
    private sealed class FakeInstanceFactory(IServiceProvider sp, ScopeCapture capture) : IUserConnectorInstanceFactory
    {
        /// <summary>
        /// Records the scope and returns a connector that owns nothing.
        /// </summary>
        /// <param name="settings">Ignored.</param>
        /// <param name="disposable">Ignored.</param>
        /// <returns>A connector that does nothing.</returns>
        public IUserConnector Create(UserSettings settings, AsyncDisposableBox disposable)
        {
            capture.Scope = sp;

            return new StubConnector();
        }
    }

    /// <summary>A connector that does nothing, so the test observes only the factory's own behaviour.</summary>
    private sealed class StubConnector : IUserConnector
    {
        /// <summary>Gets the connector's status; unused by this test.</summary>
        public ConnectorStatus Status => ConnectorStatus.Connected;

        /// <summary>Gets the asset stream; unused by this test.</summary>
        public IObservable<ChangeEvent<AssetModel>> Assets =>
            System.Reactive.Linq.Observable.Empty<ChangeEvent<AssetModel>>();

        /// <summary>Gets the position stream; unused by this test.</summary>
        public IObservable<ChangeEvent<PositionModel>> Positions =>
            System.Reactive.Linq.Observable.Empty<ChangeEvent<PositionModel>>();

        /// <summary>Gets the order stream; unused by this test.</summary>
        public IObservable<ChangeEvent<OrderModel>> Orders =>
            System.Reactive.Linq.Observable.Empty<ChangeEvent<OrderModel>>();

        /// <summary>Gets the trade stream; unused by this test.</summary>
        public IObservable<TradeModel> Trades => System.Reactive.Linq.Observable.Empty<TradeModel>();

        /// <summary>Raised on status change; unused by this test.</summary>
        public event Action<ConnectorStatus> OnStatusChanged = delegate { };

        /// <summary>Raised on error; unused by this test.</summary>
        public event Action<ConnectorError> OnError = delegate { };

        /// <summary>Raised on sync; unused by this test.</summary>
        public event Func<UserSettings, IUserProvider, Task> OnSync = delegate
        {
            return Task.CompletedTask;
        };

        /// <summary>Does nothing; unused by this test.</summary>
        public void Sync() => OnStatusChanged(ConnectorStatus.Connected);

        /// <summary>Not used by this test.</summary>
        /// <param name="position">Ignored.</param>
        /// <param name="leverage">Ignored.</param>
        /// <returns>An unsupported result.</returns>
        public Task<UserResult> SetLeverageAsync(PositionModel position, decimal leverage)
        {
            OnError(new ConnectorError("unused"));

            return Task.FromResult(UserResult.New(UserOperationStatus.UnknownError, "unused"));
        }

        /// <summary>Not used by this test.</summary>
        /// <param name="request">Ignored.</param>
        /// <returns>An unsupported result.</returns>
        public Task<UserResult<OrderModel?>> InitOrderAsync(IInitOrderRequest request) =>
            Task.FromResult(UserResult.New<OrderModel?>(UserOperationStatus.UnknownError, null, "unused"));

        /// <summary>Not used by this test.</summary>
        /// <param name="request">Ignored.</param>
        /// <returns>An unsupported result.</returns>
        public Task<UserResult<OrderModel?>> ModifyOrderAsync(IModifyOrderRequest request) =>
            Task.FromResult(UserResult.New<OrderModel?>(UserOperationStatus.UnknownError, null, "unused"));

        /// <summary>Not used by this test.</summary>
        /// <param name="request">Ignored.</param>
        /// <returns>An unsupported result.</returns>
        public Task<UserResult> CancelOrderAsync(ICancelOrderRequest request) =>
            Task.FromResult(UserResult.New(UserOperationStatus.UnknownError, "unused"));

        /// <summary>Not used by this test.</summary>
        /// <param name="symbol">Ignored.</param>
        /// <returns>An unsupported result.</returns>
        public Task<UserResult> CancelAllOrdersAsync(string symbol) =>
            Task.FromResult(UserResult.New(UserOperationStatus.UnknownError, "unused"));

        /// <summary>Does nothing.</summary>
        /// <returns>A completed task.</returns>
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
