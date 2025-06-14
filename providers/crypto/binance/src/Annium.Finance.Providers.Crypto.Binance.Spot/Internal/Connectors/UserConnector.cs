using System;
using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Connectors.Connectors;
using Annium.Finance.Providers.Abstractions.Domain.Interfaces;
using Annium.Finance.Providers.Abstractions.Domain.Models;
using Annium.Finance.Providers.Abstractions.Domain.Operations;
using Annium.Finance.Providers.Crypto.Binance.Base;
using Annium.Finance.Providers.Shared.Connectors;
using Annium.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Internal.Connectors;

internal class UserConnector : UserConnectorBase, IUserConnector
{
    public UserConnector(
        UserConfig config,
        [FromKeyedServices(Constants.Provider)] IUserProvider userProvider,
        IStatusMonitor monitor,
        ILogger logger
    )
        : base(config.GetSettings(), userProvider, monitor, logger)
    {
        // init load
        // schedule sync on connected
    }

    public ValueTask InitAsync()
    {
        return ValueTask.CompletedTask;
    }

    public Task<UserResult> SetLeverageAsync(PositionModel position, decimal leverage)
    {
        throw new NotImplementedException();
    }

    public Task<UserResult<OrderModel?>> InitOrderAsync(IInitOrderRequest request)
    {
        throw new NotImplementedException();
    }

    public Task<UserResult<OrderModel?>> ModifyOrderAsync(IModifyOrderRequest request)
    {
        throw new NotImplementedException();
    }

    public Task<UserResult> CancelOrderAsync(ICancelOrderRequest order)
    {
        throw new NotImplementedException();
    }

    public Task<UserResult> CancelAllOrdersAsync(string symbol)
    {
        throw new NotImplementedException();
    }
}
