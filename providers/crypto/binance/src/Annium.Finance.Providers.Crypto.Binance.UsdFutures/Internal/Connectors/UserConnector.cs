using System;
using System.Text;
using System.Threading.Tasks;
using Annium.Data.Tables;
using Annium.Finance.Providers.Abstractions.Connectors.Connectors;
using Annium.Finance.Providers.Abstractions.Connectors.Sync;
using Annium.Finance.Providers.Abstractions.Domain.Dto;
using Annium.Finance.Providers.Abstractions.Domain.Interfaces;
using Annium.Finance.Providers.Abstractions.Domain.Operations;
using Annium.Finance.Providers.Crypto.Binance.Base;
using Annium.Finance.Providers.Crypto.Binance.Base.Connectors;
using Annium.Finance.Providers.Crypto.Binance.Base.Connectors.Extensions;
using Annium.Finance.Providers.Crypto.Binance.Base.Contracts.Shared.Domain;
using Annium.Finance.Providers.Crypto.Binance.Base.Services;
using Annium.Finance.Providers.Shared.Connectors;
using Annium.Logging;
using Annium.Net.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Connectors;

internal class UserConnector : UserConnectorBase, IUserConnector
{
    private readonly UserConfig _config;
    private readonly IHttpRequestFactory _cancelAllOrdersRequestFactory;
    private readonly UserStream _userStream;
    private readonly QueryProcessor _queryProcessor;
    private readonly SignatureService _signatureService;

    public UserConnector(
        UserConfig config,
        [FromKeyedServices(Constants.Provider)] IUserProvider userProvider,
        QueryProcessor queryProcessor,
        SignatureService signatureService,
        [FromKeyedServices(Constants.CancelAllOrdersKey)] IHttpRequestFactory cancelAllOrdersRequestFactory,
        UserStream userStream,
        ITableFactory tableFactory,
        IStatusMonitor monitor,
        IUserSynchronizer synchronizer,
        ILogger logger
    )
        : base(config.GetSettings(), userProvider, tableFactory, monitor, synchronizer, logger)
    {
        _config = config;
        _queryProcessor = queryProcessor;
        _signatureService = signatureService;
        _cancelAllOrdersRequestFactory = cancelAllOrdersRequestFactory;
        _userStream = userStream;

        // user stream
        _userStream.OnConnected += HandleConnected;
        Disposable += () => _userStream.OnConnected -= HandleConnected;

        _userStream.OnDisconnected += HandleDisconnected;
        Disposable += () => _userStream.OnConnected -= HandleDisconnected;

        _userStream.OnMessage += HandleMessage;
        Disposable += () => _userStream.OnMessage -= HandleMessage;
    }

    public ValueTask InitAsync()
    {
        return ValueTask.CompletedTask;
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

    public async Task<UserResult> CancelAllOrders(string symbol)
    {
        if (Status is not ConnectorStatus.Connected)
        {
            return UserResult.New(UserOperationStatus.NotConnected);
        }

        var queryResult = _queryProcessor.BuildCancelAllOrdersQuery(symbol);
        if (queryResult.IsFailure)
        {
            return UserResult.From(queryResult);
        }

        await Task.CompletedTask;
        var result = await _cancelAllOrdersRequestFactory
            .New(_config.HttpApi)
            .Delete("/fapi/v1/allOpenOrders")
            .Params(queryResult.Data)
            .Sign(_signatureService)
            .AsUserResultAsync(new OperationResult(0, string.Empty));

        return UserResult.From(result);
    }

    private void HandleConnected()
    {
        this.Trace("start");
    }

    private void HandleDisconnected()
    {
        this.Trace("start");
    }

    private void HandleMessage(ReadOnlyMemory<byte> raw)
    {
        this.Trace<string>("message: {msg}", Encoding.UTF8.GetString(raw.Span));
    }
}
