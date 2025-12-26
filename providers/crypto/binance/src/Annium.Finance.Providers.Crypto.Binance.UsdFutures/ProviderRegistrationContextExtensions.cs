using System;
using Annium.Core.DependencyInjection;
using Annium.Finance.Providers.Abstractions.Domain.Shared;
using Annium.Finance.Providers.Abstractions.Domain.Temp;
using Annium.Finance.Providers.Abstractions.Domain.User;
using Annium.Finance.Providers.Core;
using Annium.Finance.Providers.Core.Shared.RateLimits;
using Annium.Finance.Providers.Core.Shared.Status;
using Annium.Finance.Providers.Core.Shared.TimeSync;
using Annium.Finance.Providers.Crypto.Binance.Base;
using Annium.Finance.Providers.Crypto.Binance.Base.Shared.TimeSync;
using Annium.Finance.Providers.Crypto.Binance.Base.User.Services;
using Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Market;
using Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Market.Contracts;
using Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Shared;
using Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Shared.Contracts;
using Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.User;
using Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.User.Contracts;
using Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.User.Services;
using Annium.Logging;
using Annium.Net.Http;
using static Annium.Finance.Providers.Crypto.Binance.UsdFutures.Constants;
using UserConfig = Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.User.UserConfig;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures;

public static class ProviderRegistrationContextExtensions
{
    public static ProviderRegistrationContext WithBinanceUsdFutures(this ProviderRegistrationContext ctx)
    {
        return ctx.WithBinanceUsdFutures(new ProviderConfiguration());
    }

    public static ProviderRegistrationContext WithBinanceUsdFutures(
        this ProviderRegistrationContext ctx,
        ProviderConfiguration cfg
    )
    {
        // provider
        var baseCfg = new ProviderBaseConfiguration(
            Provider,
            ProviderEnvironment.Real | ProviderEnvironment.Test,
            cfg.ServerTime
        );
        ctx.AddProvider<
            MarketProviderFactory,
            MarketConnectorFactory,
            UserProviderFactory,
            UserConnector,
            FinanceService
        >(baseCfg);

        // settings
        ctx.Container.Add(cfg).AsSelf().Singleton();
        ctx.Container.Add(UserConfigFactory).AsSelf().Scoped();

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
        ctx.AddBookTickerServiceFactory();
        ctx.Container.Add<QueryProcessor>().AsSelf().Singleton();
        ctx.Container.Add(RateLimiterFactory).AsSelf().Singleton();
        ctx.Container.Add(SignatureServiceFactory).AsSelf().Scoped();
        ctx.Container.Add(ListenKeyResolverFactory).AsSelf().Scoped();
        ctx.Container.Add(UserStreamFactory).AsSelf().Scoped();

        foreach (var env in baseCfg.Environments.EnumerateFlags())
        {
            var providerKey = ProviderKey.Create(Provider, env);
            ctx.Container.Add(ServerTimeProviderFactory).AsKeyed<IServerTimeProvider>(providerKey).Singleton();
        }

        return ctx;
    }

    private static IServerTimeProvider ServerTimeProviderFactory(IServiceProvider sp, object key)
    {
        var providerKey = key.CastTo<ProviderKey>();

        var requestFactory = sp.ResolveHttpRequestFactory(ServerTimeKey);
        var httpApi = Endpoints.GetHttpApi(providerKey.Environment);
        var logger = sp.Resolve<ILogger>();

        return new ServerTimeProvider(requestFactory, httpApi, "/fapi/v1/time", logger);
    }

    private static UserConfig UserConfigFactory(IServiceProvider sp)
    {
        var userSettings = sp.Resolve<Injected<UserSettings>>().Value;

        var httpApi = Endpoints.GetHttpApi(userSettings.Environment);
        var wsApi = Endpoints.GetWsApi(userSettings.Environment);

        var providerConfig = sp.Resolve<ProviderConfiguration>();

        return new UserConfig
        {
            Provider = userSettings.Provider,
            Environment = userSettings.Environment,
            Key = userSettings.Key,
            Secret = userSettings.Secret,
            HttpApi = httpApi,
            WsApi = wsApi,
            ListenKeyUriPath = "/ws/",
            ListenKey = providerConfig.ListenKey,
            ReloadContext = providerConfig.ReloadContext,
            ReloadOrders = providerConfig.ReloadOrders,
            ReloadTrades = providerConfig.ReloadTrades,
        };
    }

    private static IRateLimiter RateLimiterFactory(IServiceProvider sp)
    {
        var factory = sp.Resolve<IRateLimiterFactory>();

        return factory.CreateRateLimiter(2400, 300, 3_000);
    }

    private static SignatureService SignatureServiceFactory(IServiceProvider sp)
    {
        var userSettings = sp.Resolve<Injected<UserSettings>>().Value;
        var providerKey = userSettings.GetProviderKey();
        var serverTimeSource = sp.ResolveKeyed<IServerTimeSource>(providerKey);

        return new SignatureService(userSettings, serverTimeSource);
    }

    private static ListenKeyResolver ListenKeyResolverFactory(IServiceProvider sp)
    {
        var config = sp.Resolve<UserConfig>();
        var httpRequestFactory = sp.ResolveHttpRequestFactory(ListenKeyKey);
        var signatureService = sp.Resolve<SignatureService>();
        var statusReporter = sp.Resolve<IStatusReporter>();
        var logger = sp.Resolve<ILogger>();

        return new ListenKeyResolver(
            config,
            "/fapi/v1/listenKey",
            httpRequestFactory,
            signatureService,
            statusReporter,
            logger
        );
    }

    private static UserStream UserStreamFactory(IServiceProvider sp)
    {
        var config = sp.Resolve<UserConfig>();
        var listenKeyResolver = sp.Resolve<ListenKeyResolver>();
        var statusReporter = sp.Resolve<IStatusReporter>();
        var logger = sp.Resolve<ILogger>();

        return new UserStream(config, listenKeyResolver, statusReporter, logger);
    }
}
