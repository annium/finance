using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Annium.Core.DependencyInjection;
using Annium.Finance.Providers.Abstractions.Connectors.Connectors;
using Annium.Finance.Providers.Abstractions.Domain.Extensions;
using Annium.Finance.Providers.Abstractions.Domain.Models;
using Annium.Finance.Providers.Abstractions.Domain.Operations;
using Annium.Finance.Providers.Crypto.Binance.Base.Connectors.Extensions;
using Annium.Finance.Providers.Crypto.Binance.Base.Services;
using Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Connectors.Extensions;
using Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Contracts.User.Domain;
using Annium.Finance.Providers.Shared.Connectors;
using Annium.Finance.Providers.Shared.ServerTime;
using Annium.Logging;
using Annium.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using static Annium.Finance.Providers.Crypto.Binance.UsdFutures.Constants;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Connectors;

internal class UserProvider : UserProviderBase, IUserProvider
{
    private const int OrderQueryLimit = 1000;
    private const int TradeQueryLimit = 1000;
    private static long OrderQueryWindow { get; } = TimeSpan.FromDays(7).TotalMilliseconds.FloorInt64();
    private static long TradeQueryWindow { get; } = TimeSpan.FromDays(7).TotalMilliseconds.FloorInt64();

    private readonly IServiceProvider _sp;
    private readonly IHttpRequestFactory _getAccountRequestFactory;
    private readonly IHttpRequestFactory _getOrderRequestFactory;
    private readonly IHttpRequestFactory _getTradeRequestFactory;

    public UserProvider(
        IServiceProvider sp,
        ITimeProvider timeProvider,
        [FromKeyedServices(GetAccountKey)] IHttpRequestFactory getAccountRequestFactory,
        [FromKeyedServices(GetOrderKey)] IHttpRequestFactory getOrderRequestFactory,
        [FromKeyedServices(GetTradeKey)] IHttpRequestFactory getTradeRequestFactory,
        ILogger logger
    )
        : base(timeProvider, logger)
    {
        _sp = sp;
        _getAccountRequestFactory = getAccountRequestFactory;
        _getOrderRequestFactory = getOrderRequestFactory;
        _getTradeRequestFactory = getTradeRequestFactory;
    }

    public async Task<UserResult<UserContext?>> LoadContextAsync(UserSettings settings)
    {
        this.Trace("start");

        var signatureService = GetSignatureService(settings);

        var result = await _getAccountRequestFactory
            .New(Endpoints.GetHttpApi(settings.Environment))
            .Get("/fapi/v2/account")
            .ReceiveWindow()
            .Sign(signatureService)
            .WithRateDelay1M()
            // .WithLogFromWithHeaders(this, LogData.Headers | LogData.Response)
            .WithLogFromWithHeaders(this, LogData.Headers)
            .AsUserResultAsync<AccountResponse>();

        if (result.IsFailure)
        {
            this.Trace("failure: {result}", result);

            return UserResult.From(result, default(UserContext));
        }

        var assets = result.Data.Balances
            .Select(x => new AssetModel(x.Asset, x.Free, x.InitialMargin + x.MaintenanceMargin))
            .ToArray();

        var positions = result.Data.Positions
            .Select(x => new PositionModel(x.Symbol, x.Orientation, x.MarginType, x.Leverage, x.Amount))
            .ToArray();

        this.Trace("done");

        return UserResult.Ok<UserContext?>(new UserContext(assets, positions));
    }

    public async Task<UserResult<IReadOnlyCollection<OrderModel>?>> LoadOpenOrdersAsync(UserSettings settings)
    {
        this.Trace("start");

        var signatureService = GetSignatureService(settings);

        var result = await _getOrderRequestFactory
            .New(Endpoints.GetHttpApi(settings.Environment))
            .Get("/fapi/v1/openOrders")
            .ReceiveWindow()
            .Sign(signatureService)
            .WithRateDelay1M()
            // .WithLogFromWithHeaders(this, LogData.Headers | LogData.Response)
            .WithLogFromWithHeaders(this, LogData.Headers)
            .AsUserResultAsync<IReadOnlyCollection<OrderModel>>();

        if (result.IsFailure)
        {
            this.Trace("failure: {result}", result);

            return UserResult.From(result, default(IReadOnlyCollection<OrderModel>));
        }

        this.Trace("done");

        return UserResult.Ok<IReadOnlyCollection<OrderModel>?>(result.Data);
    }

    public async Task<UserResult<IReadOnlyCollection<OrderModel>?>> LoadOrdersAsync(
        UserSettings settings,
        IReadOnlyCollection<string> symbols,
        long? since
    )
    {
        this.Trace("start");

        var signatureService = GetSignatureService(settings);

        // load current open orders
        var result = await _getOrderRequestFactory
            .New(Endpoints.GetHttpApi(settings.Environment))
            .Get("/fapi/v1/openOrders")
            .ReceiveWindow()
            .Sign(signatureService)
            .WithRateDelay1M()
            .WithLogFromWithHeaders(this, LogData.Headers | LogData.Response)
            .AsUserResultAsync<IReadOnlyCollection<OrderModel>>();

        if (result.IsFailure)
        {
            this.Trace("failure: {result}", result);

            return UserResult.From(result, default(IReadOnlyCollection<OrderModel>));
        }

        var resolvedOrders = ResolveOrders(result.Data);

        // if no historical orders to resolve status of - just return opened ones
        if (since is null)
        {
            this.Trace("load since null - only open orders");

            return UserResult.Ok<IReadOnlyCollection<OrderModel>?>(resolvedOrders.Values);
        }

        var (startTime, endTime) = ResolveHistoryBounds(since.Value);
        var historyOrders = new List<OrderModel>();

        foreach (var symbol in symbols)
        {
            var start = startTime.ToUnixTimeMilliseconds();
            var end = endTime.ToUnixTimeMilliseconds();

            while (start < end)
            {
                var until = Math.Min(start + OrderQueryWindow, end);
                var chunkResult = await _getOrderRequestFactory
                    .New(Endpoints.GetHttpApi(settings.Environment))
                    .Get("/fapi/v1/allOrders")
                    .Param("symbol", symbol)
                    .Param("limit", OrderQueryLimit)
                    .Param("startTime", start)
                    .Param("endTime", until)
                    .ReceiveWindow()
                    .Sign(signatureService)
                    .WithRateDelay1M()
                    .WithLogFromWithHeaders(this, LogData.Headers | LogData.Response)
                    .AsUserResultAsync<IReadOnlyCollection<OrderModel>>();

                if (chunkResult.IsFailure)
                    return chunkResult;

                historyOrders.AddRange(chunkResult.Data);
                start += OrderQueryWindow;
            }
        }

        ResolveOrders(resolvedOrders, historyOrders);

        return UserResult.Ok<IReadOnlyCollection<OrderModel>?>(resolvedOrders.Values);
    }

    public async Task<UserResult<IReadOnlyCollection<TradeModel>?>> LoadTradesAsync(
        UserSettings settings,
        IReadOnlyCollection<string> symbols,
        long since
    )
    {
        this.Trace("start");

        var signatureService = GetSignatureService(settings);

        var resolvedTrades = new Dictionary<string, TradeModel>();

        var (startTime, endTime) = ResolveHistoryBounds(since);
        var historyTrades = new List<TradeModel>();

        foreach (var symbol in symbols)
        {
            var start = startTime.ToUnixTimeMilliseconds();
            var end = endTime.ToUnixTimeMilliseconds();

            while (start < end)
            {
                var until = Math.Min(start + TradeQueryWindow, end);
                var chunkResult = await _getTradeRequestFactory
                    .New(Endpoints.GetHttpApi(settings.Environment))
                    .Get("/fapi/v1/userTrades")
                    .Param("symbol", symbol)
                    .Param("limit", TradeQueryLimit)
                    .Param("startTime", start)
                    .Param("endTime", until)
                    .ReceiveWindow()
                    .Sign(signatureService)
                    .WithRateDelay1M()
                    .WithLogFromWithHeaders(this, LogData.Headers | LogData.Response)
                    .AsUserResultAsync<IReadOnlyCollection<TradeModel>>();

                if (chunkResult.IsFailure)
                    return chunkResult;

                historyTrades.AddRange(chunkResult.Data);
                start += TradeQueryWindow;
            }
        }

        ResolveTrades(resolvedTrades, historyTrades);

        return UserResult.Ok<IReadOnlyCollection<TradeModel>?>(resolvedTrades.Values);
    }

    private SignatureService GetSignatureService(UserSettings settings)
    {
        return new SignatureService(settings, _sp.ResolveKeyed<IServerTimeProvider>(settings.GetProviderKey()));
    }
}
