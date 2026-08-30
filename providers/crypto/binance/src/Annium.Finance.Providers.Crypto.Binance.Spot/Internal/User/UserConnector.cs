using System;
using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Connectors.User;
using Annium.Finance.Providers.Abstractions.Domain.User;
using Annium.Finance.Providers.Abstractions.Domain.User.Operations;
using Annium.Finance.Providers.Abstractions.Domain.User.Requests;
using Annium.Finance.Providers.Core.Shared.Status;
using Annium.Finance.Providers.Core.User;
using Annium.Finance.Providers.Crypto.Binance.Base.User;
using Annium.Logging;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Internal.User;

/// <summary>
/// Binance spot account connector. Order placement, modification and cancellation are not implemented yet;
/// spot has no leveraged positions, so <see cref="SetLeverageAsync"/> is not applicable either.
/// </summary>
internal class UserConnector : UserConnectorBase, IUserConnector
{
    /// <summary>Initializes a new instance of the <see cref="UserConnector"/> class.</summary>
    /// <param name="config">The resolved account connection settings.</param>
    /// <param name="provider">The user data provider used to load account state.</param>
    /// <param name="reporter">The status reporter used to publish connection status changes.</param>
    /// <param name="monitor">The status monitor used to detect and recover from stalled connections.</param>
    /// <param name="disposable">The disposable box collecting this connector's cleanup actions.</param>
    /// <param name="logger">The logger instance.</param>
    public UserConnector(
        UserConfig config,
        IUserProvider provider,
        IStatusReporter reporter,
        IStatusMonitor monitor,
        AsyncDisposableBox disposable,
        ILogger logger
    )
        : base(config.GetSettings(), provider, reporter, monitor, disposable, logger)
    {
        // init load
        // schedule sync on connected
    }

    /// <summary>Not implemented; spot has no leveraged positions.</summary>
    /// <param name="position">The position to change leverage for.</param>
    /// <param name="leverage">The leverage to set.</param>
    /// <returns>Does not return; always throws.</returns>
    public Task<UserResult> SetLeverageAsync(PositionModel position, decimal leverage)
    {
        throw new NotImplementedException();
    }

    /// <summary>Not implemented yet.</summary>
    /// <param name="request">The order parameters.</param>
    /// <returns>Does not return; always throws.</returns>
    public Task<UserResult<OrderModel?>> InitOrderAsync(IInitOrderRequest request)
    {
        throw new NotImplementedException();
    }

    /// <summary>Not implemented yet.</summary>
    /// <param name="request">The modification parameters, including the order being modified.</param>
    /// <returns>Does not return; always throws.</returns>
    public Task<UserResult<OrderModel?>> ModifyOrderAsync(IModifyOrderRequest request)
    {
        throw new NotImplementedException();
    }

    /// <summary>Not implemented yet.</summary>
    /// <param name="order">Identifies the order to cancel.</param>
    /// <returns>Does not return; always throws.</returns>
    public Task<UserResult> CancelOrderAsync(ICancelOrderRequest order)
    {
        throw new NotImplementedException();
    }

    /// <summary>Not implemented yet.</summary>
    /// <param name="symbol">The instrument symbol to cancel orders for.</param>
    /// <returns>Does not return; always throws.</returns>
    public Task<UserResult> CancelAllOrdersAsync(string symbol)
    {
        throw new NotImplementedException();
    }
}
