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
            this.Error("failure: {result}", result);

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
            this.Error("failure: {result}", result);

            return UserResult.From(result, default(IReadOnlyCollection<OrderModel>));
        }

        this.Trace("done");

        return UserResult.Ok<IReadOnlyCollection<OrderModel>?>(result.Data);
    }

    public async Task<UserResult<IReadOnlyCollection<OrderModel>?>> LoadOrdersAsync(
        UserSettings settings,
        string symbol,
        long? since
    )
    {
        if (since is null)
            return await LoadLatestOrdersAsync(settings, symbol);

        return await LoadOrderHistoryAsync(settings, symbol, since.Value);
    }

    public async Task<UserResult<IReadOnlyCollection<TradeModel>?>> LoadTradesAsync(
        UserSettings settings,
        string symbol,
        long? since
    )
    {
        if (since is null)
            return await LoadLatestTradesAsync(settings, symbol);

        return await LoadTradeHistoryAsync(settings, symbol, since.Value);
    }

    private async Task<UserResult<IReadOnlyCollection<OrderModel>?>> LoadLatestOrdersAsync(
        UserSettings settings,
        string symbol
    )
    {
        this.Trace("start");

        var signatureService = GetSignatureService(settings);

        var result = await _getOrderRequestFactory
            .New(Endpoints.GetHttpApi(settings.Environment))
            .Get("/fapi/v1/allOrders")
            .Param("symbol", symbol)
            .Param("limit", OrderQueryLimit)
            .ReceiveWindow()
            .Sign(signatureService)
            .WithRateDelay1M()
            .WithLogFromWithHeaders(this, LogData.Headers | LogData.Response)
            .AsUserResultAsync<IReadOnlyCollection<OrderModel>>();

        if (result.IsFailure)
        {
            this.Error("failure: {result}", result);

            return UserResult.From(result, default(IReadOnlyCollection<OrderModel>));
        }

        this.Trace("done, {count} orders loaded", result.Data.Count);

        return UserResult.Ok<IReadOnlyCollection<OrderModel>?>(result.Data);
    }

    private async Task<UserResult<IReadOnlyCollection<OrderModel>?>> LoadOrderHistoryAsync(
        UserSettings settings,
        string symbol,
        long since
    )
    {
        this.Trace("start");

        var signatureService = GetSignatureService(settings);
        var orders = new Dictionary<string, OrderModel>();
        var (startTime, endTime) = ResolveHistoryBounds(since);
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
            {
                this.Error("failure: {result}", chunkResult);

                return chunkResult;
            }

            this.Trace("chunk done, {count} orders loaded, merge", chunkResult.Data.Count);
            MergeOrders(orders, chunkResult.Data);
            start += OrderQueryWindow;
        }

        this.Trace("done, {count} orders loaded", orders.Count);

        return UserResult.Ok<IReadOnlyCollection<OrderModel>?>(orders.Values);
    }

    private async Task<UserResult<IReadOnlyCollection<TradeModel>?>> LoadLatestTradesAsync(
        UserSettings settings,
        string symbol
    )
    {
        this.Trace("start");

        var signatureService = GetSignatureService(settings);

        var result = await _getTradeRequestFactory
            .New(Endpoints.GetHttpApi(settings.Environment))
            .Get("/fapi/v1/userTrades")
            .Param("symbol", symbol)
            .Param("limit", TradeQueryLimit)
            .ReceiveWindow()
            .Sign(signatureService)
            .WithRateDelay1M()
            .WithLogFromWithHeaders(this, LogData.Headers | LogData.Response)
            .AsUserResultAsync<IReadOnlyCollection<TradeModel>>();

        if (result.IsFailure)
        {
            this.Error("failure: {result}", result);

            return UserResult.From(result, default(IReadOnlyCollection<TradeModel>));
        }

        this.Trace("done, {count} orders loaded", result.Data.Count);

        return UserResult.Ok<IReadOnlyCollection<TradeModel>?>(result.Data);
    }

    private async Task<UserResult<IReadOnlyCollection<TradeModel>?>> LoadTradeHistoryAsync(
        UserSettings settings,
        string symbol,
        long since
    )
    {
        this.Trace("start");

        var signatureService = GetSignatureService(settings);
        var trades = new Dictionary<string, TradeModel>();
        var (startTime, endTime) = ResolveHistoryBounds(since);
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
            {
                this.Error("failure: {result}", chunkResult);

                return chunkResult;
            }

            this.Trace("chunk done, {count} trades loaded, merge", chunkResult.Data.Count);
            MergeTrades(trades, chunkResult.Data);
            start += TradeQueryWindow;
        }

        this.Trace("done, {count} trades loaded", trades.Count);

        return UserResult.Ok<IReadOnlyCollection<TradeModel>?>(trades.Values);
    }

    private SignatureService GetSignatureService(UserSettings settings)
    {
        return new SignatureService(settings, _sp.ResolveKeyed<IServerTimeProvider>(settings.GetProviderKey()));
    }
}
