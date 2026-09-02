using System;
using Annium.Core.DependencyInjection;
using Annium.Finance.Providers.Abstractions.Domain.Shared;
using Annium.Finance.Providers.Core;
using Annium.Finance.Providers.Core.Shared;
using Annium.Finance.Providers.Core.Shared.RateLimits;
using Annium.Finance.Providers.Core.Shared.TimeSync;
using Annium.Finance.Providers.Crypto.Binance.Base.Shared;
using Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Market;
using Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Market.Contracts;
using Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Shared;
using Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Shared.Contracts;
using Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.User;
using Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.User.Contracts;
using Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.User.Services;
using static Annium.Finance.Providers.Crypto.Binance.UsdFutures.Constants;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures;

/// <summary>
/// Registers the Binance USD-M futures provider (market data, user connector, and all their supporting
/// services) into a <see cref="ProviderRegistrationContext"/>.
/// </summary>
public static class ProviderRegistrationContextExtensions
{
    /// <summary>
    /// Registers the Binance USD-M futures provider with default configuration.
    /// </summary>
    /// <param name="ctx">The provider registration context.</param>
    /// <returns>The same context, for chaining.</returns>
    public static ProviderRegistrationContext WithBinanceUsdFutures(this ProviderRegistrationContext ctx)
    {
        return ctx.WithBinanceUsdFutures(new ProviderConfiguration());
    }

    /// <summary>
    /// Registers the Binance USD-M futures provider: the market and user provider/connector factories, the
    /// per-endpoint HTTP request factories and JSON serializers, the shared query processor and rate limiter,
    /// and a keyed server time provider per supported environment.
    /// </summary>
    /// <param name="ctx">The provider registration context.</param>
    /// <param name="cfg">The provider configuration.</param>
    /// <returns>The same context, for chaining.</returns>
    public static ProviderRegistrationContext WithBinanceUsdFutures(
        this ProviderRegistrationContext ctx,
        ProviderConfiguration cfg
    )
    {
        // provider
        var baseCfg = new ProviderBaseConfiguration(Provider, cfg.ServerTime);
        ctx.AddProvider<
            MarketProviderFactory,
            MarketConnectorFactory,
            UserProviderFactory,
            UserConnectorFactory,
            FinanceService
        >(baseCfg);

        // settings
        ctx.Container.Add(cfg).AsSelf().Singleton();

        // serializers and http factories
        // market
        ctx.AddHttpRequestFactoryWithJsonSerializer(ExchangeInfoKey, MarketContracts.ExchangeInfo);
        ctx.AddHttpRequestFactoryWithJsonSerializer(CandleKey, MarketContracts.Candle);
        ctx.AddHttpRequestFactoryWithJsonSerializer(InstrumentTickerKey, MarketContracts.InstrumentTicker);
        ctx.AddHttpRequestFactoryWithJsonSerializer(ServerTimeKey, SharedContracts.ServerTime);

        // user data load
        ctx.AddHttpRequestFactoryWithJsonSerializer(GetAccountKey, UserContracts.GetAccount);
        ctx.AddHttpRequestFactoryWithJsonSerializer(GetOrderKey, UserContracts.GetOrder);
        ctx.AddHttpRequestFactoryWithJsonSerializer(GetTradeKey, UserContracts.GetTrade);

        // user data trade
        ctx.AddHttpRequestFactoryWithJsonSerializer(SetLeverageKey, UserContracts.SetLeverage);
        ctx.AddHttpRequestFactoryWithJsonSerializer(InitOrderKey, UserContracts.InitOrder);
        ctx.AddHttpRequestFactoryWithJsonSerializer(ModifyOrderKey, UserContracts.ModifyOrder);
        ctx.AddHttpRequestFactoryWithJsonSerializer(CancelOrderKey, UserContracts.CancelOrder);
        ctx.AddHttpRequestFactoryWithJsonSerializer(CancelAllOrdersKey, UserContracts.CancelAllOrders);

        // user data updates
        ctx.AddHttpRequestFactoryWithJsonSerializer(ListenKeyKey, UserContracts.ListenKey);
        ctx.AddJsonSerializer(AccountConfigurationUpdateKey, UserContracts.AccountConfigurationUpdate);
        ctx.AddJsonSerializer(BalanceAndPositionUpdateKey, UserContracts.BalanceAndPositionUpdate);
        ctx.AddJsonSerializer(OrderUpdateKey, UserContracts.OrderUpdate);

        // services
        ctx.Container.Add<QueryProcessor>().AsSelf().Singleton();
        ctx.Container.Add(RateLimiterFactory).AsSelf().Scoped();

        var providerKey = ProviderKey.Create(Provider);
        ctx.Container.Add(ServerTimeProviderFactory).AsKeyed<IServerTimeProvider>(providerKey).Singleton();

        return ctx;
    }

    /// <summary>
    /// Creates the server time provider for the provider's key, pointed at its server time endpoint.
    /// </summary>
    /// <param name="sp">The service provider used to resolve dependencies.</param>
    /// <param name="key">The keyed registration's provider key.</param>
    /// <returns>A new server time provider.</returns>
    private static IServerTimeProvider ServerTimeProviderFactory(IServiceProvider sp, object key)
    {
        var httpApi = Endpoints.HttpApi;

        return sp.CreateServerTimeProvider(ServerTimeKey, httpApi, Endpoints.ServerTimeUriPath);
    }

    /// <summary>
    /// Creates the rate limiter shared across the provider's HTTP requests: a 2400-weight limit that decays by
    /// 300 every 3000 milliseconds, matching Binance USD-M futures' default request weight limit.
    /// </summary>
    /// <param name="sp">The service provider used to resolve dependencies.</param>
    /// <returns>A new rate limiter configured for the provider's request weight limit.</returns>
    private static IRateLimiter RateLimiterFactory(IServiceProvider sp)
    {
        return sp.CreateRateLimiter(2400, 300, 3_000);
    }
}
