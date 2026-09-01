using System;
using System.Threading.Tasks;
using Annium.Data.Tables;
using Annium.Finance.Providers.Abstractions.Connectors.Shared;
using Annium.Finance.Providers.Abstractions.Connectors.User;
using Annium.Finance.Providers.Abstractions.Domain.User;
using Annium.Finance.Providers.Abstractions.Domain.User.Operations;
using Annium.Finance.Providers.Abstractions.Domain.User.Requests;
using Annium.Finance.Providers.Core.Internal.User;
using Annium.Finance.Providers.Tests.Lib;
using Annium.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Annium.Finance.Providers.Core.Tests.User;

/// <summary>
/// Pins the lifetime relationship between a user connector and the DI scope it was built from — the same
/// contract its market sibling has, and one this pair had covered on one side only.
/// </summary>
public class ScopedUserConnectorTests : ProvidersTestBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ScopedUserConnectorTests"/> class.
    /// </summary>
    /// <param name="outputHelper">The xUnit output helper used to capture test logs.</param>
    public ScopedUserConnectorTests(ITestOutputHelper outputHelper)
        : base(outputHelper) { }

    /// <summary>
    /// The connector is disposed while its scope is still usable, and the scope is disposed after. The user
    /// side is where the dependency is most explicit: <c>OnSync</c> hands the handler the provider itself,
    /// which is resolved from this scope.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task Disposal_TearsDownTheConnectorWhileItsScopeIsStillAlive()
    {
        // arrange
        var scope = Get<IServiceProvider>().CreateAsyncScope();
        var inner = new RecordingConnector(scope);
        var connector = new ScopedUserConnector(inner, scope);

        // act
        await connector.DisposeAsync();

        // assert
        inner.WasDisposed.IsTrue();
        inner.ScopeWasAliveOnDisposal.IsTrue("the connector must be torn down before the scope it resolved from");
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
    /// A user connector that records, at the moment it is disposed, whether its scope was still alive.
    /// </summary>
    /// <param name="scope">The scope to probe on disposal.</param>
    private sealed class RecordingConnector(AsyncServiceScope scope) : IUserConnector
    {
        /// <summary>Gets a value indicating whether this connector was disposed.</summary>
        public bool WasDisposed { get; private set; }

        /// <summary>Gets a value indicating whether the scope still served resolutions when this was disposed.</summary>
        public bool ScopeWasAliveOnDisposal { get; private set; }

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
