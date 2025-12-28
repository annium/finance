using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Connectors.User;
using Annium.Finance.Providers.Abstractions.Domain.User;
using Annium.Finance.Providers.Abstractions.Domain.User.Operations;
using Annium.Finance.Providers.Core.Shared.RateLimits;
using Annium.Finance.Providers.Core.User;
using Annium.Finance.Providers.Crypto.Binance.Base.Shared.HttpExtensions;
using Annium.Finance.Providers.Crypto.Binance.Base.User.Services;
using Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.User.Contracts.Domain;
using Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.User.HttpExtensions;
using Annium.Logging;
using Annium.Net.Http;
using NodaTime;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.User;

internal class UserProvider(
    UserConfig config,
    ITimeProvider timeProvider,
    ISignatureService signatureService,
    IHttpRequestFactory getAccountRequestFactory,
    IHttpRequestFactory getOrderRequestFactory,
    IHttpRequestFactory getTradeRequestFactory,
    IRateLimiter rateLimiter,
    ILogger logger
) : IUserProvider, ILogSubject
{
    private const int OrderQueryLimit = 1000;
    private const int TradeQueryLimit = 1000;
    private static long OrderQueryWindow { get; } = TimeSpan.FromDays(7).TotalMilliseconds.FloorInt64();
    private static long TradeQueryWindow { get; } = TimeSpan.FromDays(7).TotalMilliseconds.FloorInt64();

    public ILogger Logger { get; } = logger;

    public async Task<UserResult<UserContext?>> LoadContextAsync()
    {
        var result = await getAccountRequestFactory
            .New(config.HttpApi)
            .Get("/fapi/v2/account")
            .ReceiveWindow()
            .Sign(signatureService)
            .WithRateDelay1M(rateLimiter)
            // .WithLogFromWithHeaders(this, LogData.Headers | LogData.Response)
            .WithLogFromWithHeaders(this, LogData.Headers)
            .AsUserResultAsync<AccountResponse>();

        if (!result.IsSuccess)
        {
            if (result.IsFailure)
                this.Error("failure: {result}", result);

            return UserResult.From(result, default(UserContext));
        }

        var assets = result
            .Data.Balances.Select(x => new AssetModel(x.Asset, x.Free, x.InitialMargin + x.MaintenanceMargin))
            .ToArray();

        var positions = result
            .Data.Positions.Select(x => new PositionModel(x.Symbol, x.Orientation, x.MarginType, x.Leverage, x.Amount))
            .ToArray();

        return UserResult.Ok<UserContext?>(new UserContext(assets, positions));
    }

    public async Task<UserResult<IReadOnlyCollection<OrderModel>?>> LoadOpenOrdersAsync()
    {
        var result = await getOrderRequestFactory
            .New(config.HttpApi)
            .Get("/fapi/v1/openOrders")
            .ReceiveWindow()
            .Sign(signatureService)
            .WithRateDelay1M(rateLimiter)
            // .WithLogFromWithHeaders(this, LogData.Headers | LogData.Response)
            .WithLogFromWithHeaders(this, LogData.Headers)
            .AsUserResultAsync<IReadOnlyCollection<OrderModel>>();

        if (!result.IsSuccess)
        {
            if (result.IsFailure)
                this.Error("failure: {result}", result);

            return UserResult.From(result, default(IReadOnlyCollection<OrderModel>));
        }

        this.Trace("done, {count} orders loaded", result.Data.Count);

        return UserResult.Ok<IReadOnlyCollection<OrderModel>?>(result.Data);
    }

    public async Task<UserResult<IReadOnlyCollection<OrderModel>?>> LoadOrdersAsync(string symbol, long? since)
    {
        if (since is null)
            return await LoadLatestOrdersAsync(symbol);

        return await LoadOrderHistoryAsync(symbol, since.Value);
    }

    public async Task<UserResult<IReadOnlyCollection<TradeModel>?>> LoadTradesAsync(string symbol, long? since)
    {
        if (since is null)
            return await LoadLatestTradesAsync(symbol);

        return await LoadTradeHistoryAsync(symbol, since.Value);
    }

    private async Task<UserResult<IReadOnlyCollection<OrderModel>?>> LoadLatestOrdersAsync(string symbol)
    {
        var result = await getOrderRequestFactory
            .New(config.HttpApi)
            .Get("/fapi/v1/allOrders")
            .Param("symbol", symbol)
            .Param("limit", OrderQueryLimit)
            .ReceiveWindow()
            .Sign(signatureService)
            .WithRateDelay1M(rateLimiter)
            .WithLogFromWithHeaders(this, LogData.Headers | LogData.Response)
            .AsUserResultAsync<IReadOnlyCollection<OrderModel>>();

        if (!result.IsSuccess)
        {
            if (result.IsFailure)
                this.Error("failure: {result}", result);

            return UserResult.From(result, default(IReadOnlyCollection<OrderModel>));
        }

        this.Trace("done, {count} orders loaded", result.Data.Count);

        return UserResult.Ok<IReadOnlyCollection<OrderModel>?>(result.Data);
    }

    private async Task<UserResult<IReadOnlyCollection<OrderModel>?>> LoadOrderHistoryAsync(string symbol, long since)
    {
        var orders = new Dictionary<string, OrderModel>();
        var (startTime, endTime) = ResolveHistoryBounds(since);
        var start = startTime.ToUnixTimeMilliseconds();
        var end = endTime.ToUnixTimeMilliseconds();
        string? fromOrder = null;

        while (start < end)
        {
            var until = Math.Min(start + OrderQueryWindow, end);

            var chunkResult = await getOrderRequestFactory
                .New(config.HttpApi)
                .Get("/fapi/v1/allOrders")
                .Param("symbol", symbol)
                .Param("limit", OrderQueryLimit)
                .Param("startTime", start)
                .Param("endTime", until)
                .ReceiveWindow()
                .Sign(signatureService)
                .WithRateDelay1M(rateLimiter)
                .WithLogFromWithHeaders(this, LogData.Headers | LogData.Response)
                .AsUserResultAsync<IReadOnlyCollection<OrderModel>>();

            if (!chunkResult.IsSuccess)
            {
                if (chunkResult.IsFailure)
                    this.Error("failure: {result}", chunkResult);

                return chunkResult;
            }

            this.Trace("chunk done, {count} orders loaded, merge", chunkResult.Data.Count);
            UserProviderHelper.MergeOrders(orders, chunkResult.Data);

            if (chunkResult.Data.Count == OrderQueryLimit)
            {
                // this assumes, that orders are sorted!
                fromOrder = chunkResult.Data.Last().Id;
                this.Trace<string?>("chunk limit reached, switch to cursor based load from {orderId}", fromOrder);
                break;
            }

            // update to load next interval
            start += OrderQueryWindow;
        }

        while (fromOrder is not null)
        {
            var chunkResult = await getOrderRequestFactory
                .New(config.HttpApi)
                .Get("/fapi/v1/allOrders")
                .Param("symbol", symbol)
                .Param("limit", OrderQueryLimit)
                .Param("orderId", fromOrder)
                .ReceiveWindow()
                .Sign(signatureService)
                .WithRateDelay1M(rateLimiter)
                .WithLogFromWithHeaders(this, LogData.Headers | LogData.Response)
                .AsUserResultAsync<IReadOnlyCollection<OrderModel>>();

            if (!chunkResult.IsSuccess)
            {
                if (chunkResult.IsFailure)
                    this.Error("failure: {result}", chunkResult);

                return chunkResult;
            }

            var chunkData = chunkResult.Data.Where(x => x.CreatedAt <= end).ToArray();
            this.Trace("chunk done, {count} orders loaded, merge", chunkData.Length);
            UserProviderHelper.MergeOrders(orders, chunkData);

            if (chunkData.Length == OrderQueryLimit)
                // update to load next chunk if limit is reached by related orders
                fromOrder = chunkData.LastOrDefault()?.Id;
            else
                // break - all related orders loaded
                break;
        }

        this.Trace("done, {count} orders loaded", orders.Count);

        return UserResult.Ok<IReadOnlyCollection<OrderModel>?>(orders.Values);
    }

    private async Task<UserResult<IReadOnlyCollection<TradeModel>?>> LoadLatestTradesAsync(string symbol)
    {
        var result = await getTradeRequestFactory
            .New(config.HttpApi)
            .Get("/fapi/v1/userTrades")
            .Param("symbol", symbol)
            .Param("limit", TradeQueryLimit)
            .ReceiveWindow()
            .Sign(signatureService)
            .WithRateDelay1M(rateLimiter)
            .WithLogFromWithHeaders(this, LogData.Headers | LogData.Response)
            .AsUserResultAsync<IReadOnlyCollection<TradeModel>>();

        if (!result.IsSuccess)
        {
            if (result.IsFailure)
                this.Error("failure: {result}", result);

            return UserResult.From(result, default(IReadOnlyCollection<TradeModel>));
        }

        this.Trace("done, {count} trades loaded", result.Data.Count);

        return UserResult.Ok<IReadOnlyCollection<TradeModel>?>(result.Data);
    }

    private async Task<UserResult<IReadOnlyCollection<TradeModel>?>> LoadTradeHistoryAsync(string symbol, long since)
    {
        var trades = new Dictionary<string, TradeModel>();
        var (startTime, endTime) = ResolveHistoryBounds(since);
        var start = startTime.ToUnixTimeMilliseconds();
        var end = endTime.ToUnixTimeMilliseconds();
        string? fromTrade = null;

        while (start < end)
        {
            var until = Math.Min(start + TradeQueryWindow, end);
            var chunkResult = await getTradeRequestFactory
                .New(config.HttpApi)
                .Get("/fapi/v1/userTrades")
                .Param("symbol", symbol)
                .Param("limit", TradeQueryLimit)
                .Param("startTime", start)
                .Param("endTime", until)
                .ReceiveWindow()
                .Sign(signatureService)
                .WithRateDelay1M(rateLimiter)
                .WithLogFromWithHeaders(this, LogData.Headers | LogData.Response)
                .AsUserResultAsync<IReadOnlyCollection<TradeModel>>();

            if (!chunkResult.IsSuccess)
            {
                if (chunkResult.IsFailure)
                    this.Error("failure: {result}", chunkResult);

                return chunkResult;
            }

            this.Trace("chunk done, {count} trades loaded, merge", chunkResult.Data.Count);
            UserProviderHelper.MergeTrades(trades, chunkResult.Data);

            if (chunkResult.Data.Count == TradeQueryLimit)
            {
                // this assumes, that trades are sorted!
                fromTrade = chunkResult.Data.Last().Id;
                this.Trace<string?>("chunk limit reached, switch to cursor based load from trade {tradeId}", fromTrade);
                break;
            }

            // update to load next interval
            start += TradeQueryWindow;
        }

        while (fromTrade is not null)
        {
            var chunkResult = await getTradeRequestFactory
                .New(config.HttpApi)
                .Get("/fapi/v1/userTrades")
                .Param("symbol", symbol)
                .Param("limit", TradeQueryLimit)
                .Param("fromId", fromTrade)
                .ReceiveWindow()
                .Sign(signatureService)
                .WithRateDelay1M(rateLimiter)
                .WithLogFromWithHeaders(this, LogData.Headers | LogData.Response)
                .AsUserResultAsync<IReadOnlyCollection<TradeModel>>();

            if (!chunkResult.IsSuccess)
            {
                if (chunkResult.IsFailure)
                    this.Error("failure: {result}", chunkResult);

                return chunkResult;
            }

            var chunkData = chunkResult.Data.Where(x => x.Moment <= end).ToArray();
            this.Trace("chunk done, {count} trades loaded, merge", chunkData.Length);
            UserProviderHelper.MergeTrades(trades, chunkData);

            if (chunkData.Length == TradeQueryLimit)
                // update to load next chunk if limit is reached by related trades
                fromTrade = chunkData.LastOrDefault()?.Id;
            else
                // break - all related orders loaded
                break;
        }

        this.Trace("done, {count} trades loaded", trades.Count);

        return UserResult.Ok<IReadOnlyCollection<TradeModel>?>(trades.Values);
    }

    private (Instant min, Instant max) ResolveHistoryBounds(long since)
    {
        var now = timeProvider.Now;
        var instant = Instant.FromUnixTimeMilliseconds(since);

        return UserProviderHelper.ResolveHistoryBounds(instant, now);
    }
}
