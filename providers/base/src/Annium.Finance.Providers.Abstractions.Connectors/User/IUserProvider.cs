using System.Collections.Generic;
using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Domain.User;
using Annium.Finance.Providers.Abstractions.Domain.User.Operations;

namespace Annium.Finance.Providers.Abstractions.Connectors.User;

/// <summary>
/// Low-level data access to a provider account: resolves account state, orders and trades. Consumed by an
/// <see cref="IUserConnector"/> implementation rather than directly by application code.
/// </summary>
public interface IUserProvider
{
    /// <summary>
    /// Loads the user context (account assets and positions) from the provider.
    /// </summary>
    /// <returns>A result carrying the user context on success, or null data with a non-success status on failure.</returns>
    Task<UserResult<UserContext?>> LoadContextAsync();

    /// <summary>
    /// Loads all currently open orders across instruments.
    /// </summary>
    /// <returns>A result carrying the open orders on success, or null data with a non-success status on failure.</returns>
    Task<UserResult<IReadOnlyCollection<OrderModel>?>> LoadOpenOrdersAsync();

    /// <summary>
    /// Loads orders for a symbol. When <paramref name="since"/> is null, loads the latest orders; otherwise loads
    /// order history starting after the given cursor.
    /// </summary>
    /// <param name="symbol">The instrument symbol to load orders for.</param>
    /// <param name="since">The exclusive order id cursor to load history from, or null to load the latest orders.</param>
    /// <returns>A result carrying the matching orders on success, or null data with a non-success status on failure.</returns>
    Task<UserResult<IReadOnlyCollection<OrderModel>?>> LoadOrdersAsync(string symbol, long? since);

    /// <summary>
    /// Loads trades for a symbol. When <paramref name="since"/> is null, loads the latest trades; otherwise loads
    /// trade history starting after the given cursor.
    /// </summary>
    /// <param name="symbol">The instrument symbol to load trades for.</param>
    /// <param name="since">The exclusive trade id cursor to load history from, or null to load the latest trades.</param>
    /// <returns>A result carrying the matching trades on success, or null data with a non-success status on failure.</returns>
    Task<UserResult<IReadOnlyCollection<TradeModel>?>> LoadTradesAsync(string symbol, long? since);
}
