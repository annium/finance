using System;
using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Connectors.Connectors;
using Annium.Finance.Providers.Abstractions.Domain.Dto;
using Annium.Finance.Providers.Abstractions.Domain.Interfaces;
using Annium.Finance.Providers.Abstractions.Domain.Operations;
using Annium.Finance.Providers.Crypto.Binance.Base;
using Annium.Finance.Providers.Crypto.Binance.Base.Services;
using Annium.Finance.Providers.Shared.Connectors;
using Annium.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Internal.Connectors;

internal class UserConnector : UserConnectorBase, IUserConnector
{
    private readonly QueryProcessor _queryProcessor;
    private readonly SignatureService _signatureService;

    public UserConnector(
        UserConfig config,
        QueryProcessor queryProcessor,
        SignatureService signatureService,
        [FromKeyedServices(Constants.Provider)] IUserProvider userProvider,
        IStatusMonitor monitor,
        ILogger logger
    )
        : base(config.GetSettings(), userProvider, monitor, logger)
    {
        _queryProcessor = queryProcessor;
        _signatureService = signatureService;
        // init load
        // schedule sync on connected
    }

    public ValueTask InitAsync()
    {
        return ValueTask.CompletedTask;
    }

    public Task<UserResult> SetLeverage(PositionDto position, decimal leverage)
    {
        throw new NotImplementedException();
    }

    public Task<UserResult<OrderDto>> InitOrder(IInitOrderRequest request)
    {
        throw new NotImplementedException();
    }

    public Task<UserResult<OrderDto>> ModifyOrder(IModifyOrderRequest request)
    {
        throw new NotImplementedException();
    }

    public Task<UserResult> CancelOrder(OrderDto order)
    {
        throw new NotImplementedException();
    }

    public Task<UserResult> CancelAllOrders(string symbol)
    {
        throw new NotImplementedException();
    }
}
