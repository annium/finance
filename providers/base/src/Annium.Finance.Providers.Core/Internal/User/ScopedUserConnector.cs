using System;
using System.Threading.Tasks;
using Annium.Data.Tables;
using Annium.Finance.Providers.Abstractions.Connectors.Shared;
using Annium.Finance.Providers.Abstractions.Connectors.User;
using Annium.Finance.Providers.Abstractions.Domain.User;
using Annium.Finance.Providers.Abstractions.Domain.User.Operations;
using Annium.Finance.Providers.Abstractions.Domain.User.Requests;
using Microsoft.Extensions.DependencyInjection;

namespace Annium.Finance.Providers.Core.Internal.User;

/// <summary>
/// A user connector together with the DI scope it was built from, so the scope outlives it.
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
internal sealed class ScopedUserConnector(IUserConnector inner, AsyncServiceScope scope) : IUserConnector
{
    /// <summary>Gets the current connection status of the connector.</summary>
    public ConnectorStatus Status => inner.Status;

    /// <summary>An observable stream of asset balance changes.</summary>
    public IObservable<ChangeEvent<AssetModel>> Assets => inner.Assets;

    /// <summary>An observable stream of position changes.</summary>
    public IObservable<ChangeEvent<PositionModel>> Positions => inner.Positions;

    /// <summary>An observable stream of order changes.</summary>
    public IObservable<ChangeEvent<OrderModel>> Orders => inner.Orders;

    /// <summary>An observable stream of executed trades.</summary>
    public IObservable<TradeModel> Trades => inner.Trades;

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

    /// <summary>Raised during a sync cycle, before the connector resumes real-time updates.</summary>
    public event Func<UserSettings, IUserProvider, Task> OnSync
    {
        add => inner.OnSync += value;
        remove => inner.OnSync -= value;
    }

    /// <summary>Forces a resync.</summary>
    public void Sync() => inner.Sync();

    /// <summary>Sets the leverage on a position.</summary>
    /// <param name="position">The position to set leverage on.</param>
    /// <param name="leverage">The leverage to set.</param>
    /// <returns>The outcome of the operation.</returns>
    public Task<UserResult> SetLeverageAsync(PositionModel position, decimal leverage) =>
        inner.SetLeverageAsync(position, leverage);

    /// <summary>Places an order.</summary>
    /// <param name="request">The order to place.</param>
    /// <returns>The placed order, or the failure that prevented it.</returns>
    public Task<UserResult<OrderModel?>> InitOrderAsync(IInitOrderRequest request) => inner.InitOrderAsync(request);

    /// <summary>Modifies an existing order.</summary>
    /// <param name="request">The modification to apply.</param>
    /// <returns>The modified order, or the failure that prevented it.</returns>
    public Task<UserResult<OrderModel?>> ModifyOrderAsync(IModifyOrderRequest request) =>
        inner.ModifyOrderAsync(request);

    /// <summary>Cancels an order.</summary>
    /// <param name="request">The order to cancel.</param>
    /// <returns>The outcome of the operation.</returns>
    public Task<UserResult> CancelOrderAsync(ICancelOrderRequest request) => inner.CancelOrderAsync(request);

    /// <summary>Cancels every open order on a symbol.</summary>
    /// <param name="symbol">The symbol to cancel orders on.</param>
    /// <returns>The outcome of the operation.</returns>
    public Task<UserResult> CancelAllOrdersAsync(string symbol) => inner.CancelAllOrdersAsync(symbol);

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
