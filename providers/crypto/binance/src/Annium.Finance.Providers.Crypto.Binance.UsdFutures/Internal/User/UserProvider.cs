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

/// <summary>
/// Loads USD-M futures account state, orders and trades straight from the Binance REST API, signing every
/// request with the account's API secret.
/// </summary>
/// <param name="config">The resolved user connector configuration.</param>
/// <param name="timeProvider">Supplies the current time, used to bound history queries.</param>
/// <param name="signatureService">Signs outgoing REST requests.</param>
/// <param name="getAccountRequestFactory">Factory for requests against the account info endpoint.</param>
/// <param name="getOrderRequestFactory">Factory for requests against the order lookup endpoints.</param>
/// <param name="getTradeRequestFactory">Factory for requests against the trade lookup endpoint.</param>
/// <param name="rateLimiter">Limits request weight against the exchange's rate limits.</param>
/// <param name="logger">The logger.</param>
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
    /// <summary>The maximum number of orders returned by a single order history request.</summary>
    private const int OrderQueryLimit = 1000;

    /// <summary>The maximum number of trades returned by a single trade history request.</summary>
    private const int TradeQueryLimit = 1000;

    /// <summary>The maximum time span covered by a single order history request (7 days).</summary>
    private static long OrderQueryWindow { get; } = TimeSpan.FromDays(7).TotalMilliseconds.FloorInt64();

    /// <summary>The maximum time span covered by a single trade history request (7 days).</summary>
    private static long TradeQueryWindow { get; } = TimeSpan.FromDays(7).TotalMilliseconds.FloorInt64();

    /// <summary>Gets the logger for this provider.</summary>
    public ILogger Logger { get; } = logger;

    /// <summary>
    /// Loads the account's asset balances and positions and maps them into the library's generic account
    /// context. An asset's usable balance is the free balance, and its locked balance is the sum of initial and
    /// maintenance margin.
    /// </summary>
    /// <returns>A result carrying the resolved account context, or a failure status if it could not be loaded.</returns>
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

    /// <summary>
    /// Loads all currently open orders, across all symbols.
    /// </summary>
    /// <returns>A result carrying the open orders, or a failure status if they could not be loaded.</returns>
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

    /// <summary>
    /// Loads orders for a symbol: the most recent page when <paramref name="since"/> is null, or the full order
    /// history from that moment onward otherwise.
    /// </summary>
    /// <param name="symbol">The instrument symbol to load orders for.</param>
    /// <param name="since">The timestamp to load orders since, in Unix milliseconds, or null for the latest page.</param>
    /// <returns>A result carrying the orders, or a failure status if they could not be loaded.</returns>
    public async Task<UserResult<IReadOnlyCollection<OrderModel>?>> LoadOrdersAsync(string symbol, long? since)
    {
        if (since is null)
            return await LoadLatestOrdersAsync(symbol);

        return await LoadOrderHistoryAsync(symbol, since.Value);
    }

    /// <summary>
    /// Loads trades for a symbol: the most recent page when <paramref name="since"/> is null, or the full trade
    /// history from that moment onward otherwise.
    /// </summary>
    /// <param name="symbol">The instrument symbol to load trades for.</param>
    /// <param name="since">The timestamp to load trades since, in Unix milliseconds, or null for the latest page.</param>
    /// <returns>A result carrying the trades, or a failure status if they could not be loaded.</returns>
    public async Task<UserResult<IReadOnlyCollection<TradeModel>?>> LoadTradesAsync(string symbol, long? since)
    {
        if (since is null)
            return await LoadLatestTradesAsync(symbol);

        return await LoadTradeHistoryAsync(symbol, since.Value);
    }

    /// <summary>
    /// Loads the most recent page of orders for a symbol, up to <see cref="OrderQueryLimit"/> orders.
    /// </summary>
    /// <param name="symbol">The instrument symbol to load orders for.</param>
    /// <returns>A result carrying the orders, or a failure status if they could not be loaded.</returns>
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

    /// <summary>
    /// Loads the full order history for a symbol from the given moment onward, paging by
    /// <see cref="OrderQueryWindow"/>-sized time windows and, once a window returns a full
    /// <see cref="OrderQueryLimit"/> page, switching to cursor-based paging by order id to drain the remainder.
    /// </summary>
    /// <param name="symbol">The instrument symbol to load orders for.</param>
    /// <param name="since">The timestamp to load orders since, in Unix milliseconds.</param>
    /// <returns>A result carrying the orders, or a failure status if any page could not be loaded.</returns>
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

    /// <summary>
    /// Loads the most recent page of trades for a symbol, up to <see cref="TradeQueryLimit"/> trades.
    /// </summary>
    /// <param name="symbol">The instrument symbol to load trades for.</param>
    /// <returns>A result carrying the trades, or a failure status if they could not be loaded.</returns>
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

    /// <summary>
    /// Loads the full trade history for a symbol from the given moment onward, paging by
    /// <see cref="TradeQueryWindow"/>-sized time windows and, once a window returns a full
    /// <see cref="TradeQueryLimit"/> page, switching to cursor-based paging by trade id to drain the remainder.
    /// </summary>
    /// <param name="symbol">The instrument symbol to load trades for.</param>
    /// <param name="since">The timestamp to load trades since, in Unix milliseconds.</param>
    /// <returns>A result carrying the trades, or a failure status if any page could not be loaded.</returns>
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

    /// <summary>
    /// Widens the requested history range into safe query bounds, padding the lower bound backwards and using
    /// the current time as the upper bound, to tolerate clock skew between this process and the exchange.
    /// </summary>
    /// <param name="since">The requested history start, in Unix milliseconds.</param>
    /// <returns>The widened start and end instants of the history range.</returns>
    private (Instant min, Instant max) ResolveHistoryBounds(long since)
    {
        var now = timeProvider.Now;
        var instant = Instant.FromUnixTimeMilliseconds(since);

        return UserProviderHelper.ResolveHistoryBounds(instant, now);
    }
}
