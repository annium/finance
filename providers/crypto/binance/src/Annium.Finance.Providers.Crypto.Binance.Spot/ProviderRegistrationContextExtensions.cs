using System;
using Annium.Core.DependencyInjection;
using Annium.Finance.Providers.Abstractions.Domain.Shared;
using Annium.Finance.Providers.Core;
using Annium.Finance.Providers.Core.Shared;
using Annium.Finance.Providers.Core.Shared.RateLimits;
using Annium.Finance.Providers.Core.Shared.TimeSync;
using Annium.Finance.Providers.Crypto.Binance.Base.Shared;
using Annium.Finance.Providers.Crypto.Binance.Spot.Internal.Market;
using Annium.Finance.Providers.Crypto.Binance.Spot.Internal.Market.Contracts;
using Annium.Finance.Providers.Crypto.Binance.Spot.Internal.Shared;
using Annium.Finance.Providers.Crypto.Binance.Spot.Internal.Shared.Contracts;
using Annium.Finance.Providers.Crypto.Binance.Spot.Internal.User;
using Annium.Finance.Providers.Crypto.Binance.Spot.Internal.User.Contracts;
using Annium.Finance.Providers.Crypto.Binance.Spot.Internal.User.Services;
using static Annium.Finance.Providers.Crypto.Binance.Spot.Constants;

namespace Annium.Finance.Providers.Crypto.Binance.Spot;

/// <summary>Registers the Binance spot provider (market data and account/trading connectors) into a provider registration context.</summary>
public static class ProviderRegistrationContextExtensions
{
    /// <summary>Registers the Binance spot provider with default timing settings.</summary>
    /// <param name="ctx">The provider registration context to register into.</param>
    /// <returns>The same context, for chaining.</returns>
    public static ProviderRegistrationContext WithBinanceSpot(this ProviderRegistrationContext ctx)
    {
        return ctx.WithBinanceSpot(new ProviderConfiguration());
    }

    /// <summary>
    /// Registers the Binance spot provider: its market and user connector/provider factories, the finance
    /// service, HTTP request factories and JSON serializers for every endpoint, and the per-environment server
    /// time provider and rate limiter.
    /// </summary>
    /// <param name="ctx">The provider registration context to register into.</param>
    /// <param name="cfg">The provider timing settings to use.</param>
    /// <returns>The same context, for chaining.</returns>
    public static ProviderRegistrationContext WithBinanceSpot(
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
        ctx.AddHttpRequestFactoryWithJsonSerializer(InitOrderKey, UserContracts.InitOrder);
        ctx.AddHttpRequestFactoryWithJsonSerializer(ModifyOrderKey, UserContracts.ModifyOrder);
        ctx.AddHttpRequestFactoryWithJsonSerializer(CancelOrderKey, UserContracts.CancelOrder);
        ctx.AddHttpRequestFactoryWithJsonSerializer(CancelAllOrdersKey, UserContracts.CancelAllOrders);

        // user data updates
        ctx.AddHttpRequestFactoryWithJsonSerializer(ListenKeyKey, UserContracts.ListenKey);
        ctx.AddJsonSerializer(AccountUpdateKey, UserContracts.AccountUpdate);
        ctx.AddJsonSerializer(OrderUpdateKey, UserContracts.OrderUpdate);

        // services
        ctx.Container.Add<QueryProcessor>().AsSelf().Singleton();
        ctx.Container.Add(RateLimiterFactory).AsSelf().Scoped();

        var providerKey = ProviderKey.Create(Provider);
        ctx.Container.Add(ServerTimeProviderFactory).AsKeyed<IServerTimeProvider>(providerKey).Singleton();

        return ctx;
    }

    /// <summary>Creates the keyed server time provider for a given provider/environment combination.</summary>
    /// <param name="sp">The service provider used to resolve dependencies.</param>
    /// <param name="key">The <see cref="ProviderKey"/> identifying the provider and environment.</param>
    /// <returns>The created server time provider.</returns>
    private static IServerTimeProvider ServerTimeProviderFactory(IServiceProvider sp, object key)
    {
        var httpApi = Endpoints.HttpApi;

        return sp.CreateServerTimeProvider(ServerTimeKey, httpApi, Endpoints.ServerTimeUriPath);
    }

    /// <summary>Creates the rate limiter shared across all Binance spot requests, matching Binance's request weight limit.</summary>
    /// <param name="sp">The service provider used to resolve dependencies.</param>
    /// <returns>The created rate limiter.</returns>
    private static IRateLimiter RateLimiterFactory(IServiceProvider sp)
    {
        return sp.CreateRateLimiter(6000, 300, 3_000);
    }
}
