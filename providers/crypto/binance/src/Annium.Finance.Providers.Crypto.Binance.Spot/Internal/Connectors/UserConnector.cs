using System;
using System.Threading.Tasks;
using Annium.Core.DependencyInjection;
using Annium.Data.Tables;
using Annium.Finance.Providers.Abstractions.Connectors.Connectors;
using Annium.Finance.Providers.Abstractions.Connectors.Sync;
using Annium.Finance.Providers.Abstractions.Domain.Dto;
using Annium.Finance.Providers.Abstractions.Domain.Interfaces;
using Annium.Finance.Providers.Abstractions.Domain.Operations;
using Annium.Finance.Providers.Crypto.Binance.Base;
using Annium.Finance.Providers.Crypto.Binance.Base.Services;
using Annium.Finance.Providers.Shared.Connectors;
using Annium.Logging;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Internal.Connectors;

internal class UserConnector : UserConnectorBase, IUserConnector
{
    private readonly QueryProcessor _queryProcessor;
    private readonly SignatureService _signatureService;

    public UserConnector(
        UserConfig config,
        IIndex<string, IUserProvider> userProviders,
        QueryProcessor queryProcessor,
        SignatureService signatureService,
        ITableFactory tableFactory,
        IStatusMonitor monitor,
        IUserSynchronizer synchronizer,
        ILogger logger
    )
        : base(config.GetSettings(), userProviders[Constants.Provider], tableFactory, monitor, synchronizer, logger)
    {
        _queryProcessor = queryProcessor;
        _signatureService = signatureService;
        // init load
        // schedule sync on connected
    }

    public ValueTask InitAsync()
    {
        throw new NotImplementedException();
    }

    public ValueTask DisposeAsync()
    {
        throw new NotImplementedException();
    }

    public Task<UserResult> SetLeverage(PositionDto position, byte leverage)
    {
        throw new NotImplementedException();
    }

    public Task<UserResult<OrderDto>> InitOrder(IInitOrderRequest order)
    {
        throw new NotImplementedException();
    }

    public Task<UserResult<OrderDto>> ModifyOrder(IModifyOrderRequest order)
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
