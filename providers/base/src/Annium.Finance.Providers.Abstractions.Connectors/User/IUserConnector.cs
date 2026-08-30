using System;
using System.Threading.Tasks;
using Annium.Data.Tables;
using Annium.Finance.Providers.Abstractions.Connectors.Shared;
using Annium.Finance.Providers.Abstractions.Domain.User;
using Annium.Finance.Providers.Abstractions.Domain.User.Operations;
using Annium.Finance.Providers.Abstractions.Domain.User.Requests;

namespace Annium.Finance.Providers.Abstractions.Connectors.User;

/// <summary>
/// Live connection to a provider account: streams account state (assets, positions, orders, trades) and lets
/// callers manage leverage and orders.
/// </summary>
public interface IUserConnector : IConnectorBase
{
    /// <summary>
    /// An observable stream of asset balance changes. Emits an <c>Init</c> event with the full asset set on
    /// (re)sync, and <c>Set</c>/<c>Delete</c> events as individual asset balances change afterwards.
    /// </summary>
    IObservable<ChangeEvent<AssetModel>> Assets { get; }

    /// <summary>
    /// An observable stream of position changes. Emits an <c>Init</c> event with the full position set on
    /// (re)sync, and <c>Set</c>/<c>Delete</c> events as individual positions change afterwards.
    /// </summary>
    IObservable<ChangeEvent<PositionModel>> Positions { get; }

    /// <summary>
    /// An observable stream of order changes. Emits an <c>Init</c> event with the currently open orders on
    /// (re)sync, then a <c>Set</c> event whenever an order is placed or updated while still open, and a
    /// <c>Delete</c> event once it stops being open (filled, canceled, rejected, expired).
    /// </summary>
    IObservable<ChangeEvent<OrderModel>> Orders { get; }

    /// <summary>
    /// An observable stream of executed trades. Emits a trade as soon as it is reported by the provider.
    /// </summary>
    IObservable<TradeModel> Trades { get; }

    /// <summary>
    /// Raised during a sync cycle, after the connector stops forwarding real-time updates and before it resumes
    /// them and reports itself as connected. Handlers receive the active settings and the underlying provider so
    /// they can (re)load account state before the connector goes live; the connector waits for the handler to
    /// complete.
    /// </summary>
    event Func<UserSettings, IUserProvider, Task> OnSync;

    /// <summary>
    /// Forces a resync: the connector reports itself as connecting, fires <see cref="OnSync"/> so account state
    /// can be reloaded, and reports itself as connected again.
    /// </summary>
    void Sync();

    /// <summary>
    /// Sets the leverage used for a position.
    /// </summary>
    /// <param name="position">The position to change leverage for.</param>
    /// <param name="leverage">The leverage to set.</param>
    /// <returns>A result indicating whether the leverage change succeeded.</returns>
    Task<UserResult> SetLeverageAsync(PositionModel position, decimal leverage);

    /// <summary>
    /// Places a new order.
    /// </summary>
    /// <param name="request">The order parameters.</param>
    /// <returns>A result carrying the placed order on success, or null data with a non-success status on failure.</returns>
    Task<UserResult<OrderModel?>> InitOrderAsync(IInitOrderRequest request);

    /// <summary>
    /// Modifies an existing order. Depending on the order type and the provider, this may amend the order in
    /// place or cancel it and place a new one.
    /// </summary>
    /// <param name="request">The modification parameters, including the order being modified.</param>
    /// <returns>A result carrying the resulting order on success, or null data with a non-success status on failure.</returns>
    Task<UserResult<OrderModel?>> ModifyOrderAsync(IModifyOrderRequest request);

    /// <summary>
    /// Cancels an existing order.
    /// </summary>
    /// <param name="request">Identifies the order to cancel.</param>
    /// <returns>A result indicating whether the cancellation succeeded.</returns>
    Task<UserResult> CancelOrderAsync(ICancelOrderRequest request);

    /// <summary>
    /// Cancels all open orders for the given symbol.
    /// </summary>
    /// <param name="symbol">The instrument symbol to cancel orders for.</param>
    /// <returns>A result indicating whether the cancellation succeeded.</returns>
    Task<UserResult> CancelAllOrdersAsync(string symbol);
}
