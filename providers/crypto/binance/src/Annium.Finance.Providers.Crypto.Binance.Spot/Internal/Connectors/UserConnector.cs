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
using Microsoft.Extensions.DependencyInjection;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Internal.Connectors;

internal class UserConnector : UserConnectorBase, IUserConnector
{
    public UserConnector(
        UserConfig config,
        [FromKeyedServices(Constants.Provider)] IUserProvider userProvider,
        IStatusReporter reporter,
        IStatusMonitor monitor,
        ILogger logger
    )
        : base(config.GetSettings(), userProvider, reporter, monitor, logger)
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
