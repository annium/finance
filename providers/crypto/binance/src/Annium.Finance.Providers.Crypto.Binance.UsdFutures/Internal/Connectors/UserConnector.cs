using System;
using System.Text;
using System.Threading;
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
using Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Connectors.Extensions;
using Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Contracts.User.Domain;
using Annium.Finance.Providers.Shared.Connectors;
using Annium.Finance.Providers.Shared.Internal.Services;
using Annium.Finance.Providers.Shared.Services;
using Annium.Logging;
using Annium.Net.Http;
using Annium.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Connectors;

internal class UserConnector : UserConnectorBase, IUserConnector
{
    private readonly UserConfig _config;
    private readonly IHttpRequestFactory _cancelAllOrdersRequestFactory;
    private readonly UserStream _userStream;
    private readonly QueryProcessor _queryProcessor;
    private readonly SignatureService _signatureService;
    private readonly IHttpRequestFactory _getAccountRequestFactory;
    private readonly ICompositeLoader<AccountResponse> _accountLoader;

    public UserConnector(
        UserConfig config,
        [FromKeyedServices(Constants.Provider)] IUserProvider userProvider,
        QueryProcessor queryProcessor,
        SignatureService signatureService,
        [FromKeyedServices(Constants.GetAccount)] IHttpRequestFactory getAccountRequestFactory,
        [FromKeyedServices(Constants.CancelAllOrdersKey)] IHttpRequestFactory cancelAllOrdersRequestFactory,
        UserStream userStream,
        ILoaderFactory loaderFactory,
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
        _getAccountRequestFactory = getAccountRequestFactory;
        _cancelAllOrdersRequestFactory = cancelAllOrdersRequestFactory;
        _userStream = userStream;

        // user stream
        _userStream.OnConnected += HandleConnected;
        Disposable += () => _userStream.OnConnected -= HandleConnected;

        _userStream.OnDisconnected += HandleDisconnected;
        Disposable += () => _userStream.OnConnected -= HandleDisconnected;

        _userStream.OnMessage += HandleMessage;
        Disposable += () => _userStream.OnMessage -= HandleMessage;

        // accounts
        Disposable += _accountLoader = loaderFactory.CreateCompositeLoader(
            new SnapshotLoaderConfig(_config.ReloadAccountInterval, _config.ReloadAccountInterval, 0),
            LoadAccount,
            _config.ReloadAccountInterval,
            _config.ReloadAccountDebounce
        );
        _accountLoader.OnData += HandleAccount;
        Disposable += () => _accountLoader.OnData -= HandleAccount;
    }

    public ValueTask InitAsync()
    {
        return ValueTask.CompletedTask;
    }

    public async Task<UserResult> SetLeverage(PositionDto position, byte leverage)
    {
        this.Trace("set leverage to {leverage}", leverage);

        await Task.CompletedTask;
        // var result = await _
        //     .New(_httpApiEndpoint)
        //     .Post("/fapi/v1/leverage")
        //     .Param("symbol", position.Instrument.Symbol)
        //     .Param("leverage", leverage)
        //     .Sign(Config.Key, Config.Secret)
        //     .WithLogFrom(this, LogData.Headers | LogData.Response)
        //     .WithRateDelay("1m")
        //     .AsUserResultAsync<LeverageResponse>();
        //
        // RequestUpdateAssetsAndPositions();

        this.Trace("done");

        return UserResult.Ok();
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
            this.Trace<string>("skip on {symbol} - not connected", symbol);
            return UserResult.New(UserOperationStatus.NotConnected);
        }

        var queryResult = _queryProcessor.BuildCancelAllOrdersQuery(symbol);
        if (queryResult.IsFailure)
        {
            this.Trace("query processing failed: {result}", queryResult);
            return UserResult.From(queryResult);
        }

        this.Trace("start");
        var result = await _cancelAllOrdersRequestFactory
            .New(_config.HttpApi)
            .Delete("/fapi/v1/allOpenOrders")
            .Params(queryResult.Data)
            .Sign(_signatureService)
            .WithLogFrom(this)
            .AsUserResultAsync(new OperationResult(0, string.Empty));
        this.Trace("done");

        return UserResult.From(result);
    }

    private async Task<IBaseResult<AccountResponse>> LoadAccount(CancellationToken ct)
    {
        this.Trace("start");
        var result = await _getAccountRequestFactory
            .New(_config.HttpApi)
            .Get("/fapi/v2/account")
            .WithRateDelay1M()
            .ReceiveWindow()
            .Sign(_signatureService)
            .WithLogFrom(this)
            .AsUserResultAsync(
                new AccountResponse(Array.Empty<AccountResponseBalance>(), Array.Empty<AccountResponsePosition>())
            );
        this.Trace("done");

        return result;
    }

    private void HandleAccount(AccountResponse response)
    {
        this.Trace("start");

        foreach (var x in response.Balances)
        {
            var asset = new AssetDto(x.Asset, x.Free, x.InitialMargin + x.MaintenanceMargin);
            AssetWriter.Write(asset);
        }

        foreach (var x in response.Positions)
        {
            var position = new PositionDto(x.Symbol, x.Orientation, x.MarginType, x.Leverage, x.Amount);
            PositionWriter.Write(position);
        }

        this.Trace("done");
    }

    private void HandleConnected()
    {
        this.Trace("start");

        _accountLoader.Start();

        this.Trace("done");
    }

    private void HandleDisconnected()
    {
        this.Trace("start");

        _accountLoader.Stop();

        this.Trace("done");
    }

    private void HandleMessage(ReadOnlyMemory<byte> raw)
    {
        this.Trace<string>("message: {msg}", Encoding.UTF8.GetString(raw.Span));
    }
}
