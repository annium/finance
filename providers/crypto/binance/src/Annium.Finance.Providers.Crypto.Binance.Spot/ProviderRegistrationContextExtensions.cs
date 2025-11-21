using System;
using System.Net.Mime;
using Annium.Core.DependencyInjection;
using Annium.Finance.Providers.Abstractions.Domain.Enums;
using Annium.Finance.Providers.Abstractions.Domain.Models;
using Annium.Finance.Providers.Crypto.Binance.Base.Connectors;
using Annium.Finance.Providers.Crypto.Binance.Base.Services;
using Annium.Finance.Providers.Crypto.Binance.Spot.Internal;
using Annium.Finance.Providers.Crypto.Binance.Spot.Internal.Connectors;
using Annium.Finance.Providers.Crypto.Binance.Spot.Internal.Contracts;
using Annium.Finance.Providers.Crypto.Binance.Spot.Internal.Services;
using Annium.Finance.Providers.Shared;
using Annium.Finance.Providers.Shared.Connectors;
using Annium.Finance.Providers.Shared.ServerTime;
using Annium.Logging;
using Annium.Net.Http;
using Annium.Serialization.Abstractions;
using static Annium.Finance.Providers.Crypto.Binance.Spot.Constants;
using MarketConfig = Annium.Finance.Providers.Crypto.Binance.Spot.Internal.MarketConfig;
using UserConfig = Annium.Finance.Providers.Crypto.Binance.Spot.Internal.UserConfig;

namespace Annium.Finance.Providers.Crypto.Binance.Spot;

public static class ProviderRegistrationContextExtensions
{
    public static ProviderRegistrationContext WithBinanceSpot(this ProviderRegistrationContext ctx)
    {
        return ctx.WithBinanceSpot(new ProviderConfiguration());
    }

    public static ProviderRegistrationContext WithBinanceSpot(
        this ProviderRegistrationContext ctx,
        ProviderConfiguration cfg
    )
    {
        // provider
        ctx.AddProvider<MarketProvider, MarketConnector, UserProvider, QueryProcessor, UserConnector, FinanceService>(
            ServerTimeProviderFactory,
            Provider,
            ProviderEnvironment.Real | ProviderEnvironment.Test
        );

        // settings
        ctx.Container.Add(cfg).AsSelf().Singleton();
        ctx.Container.Add(MarketConfigFactory).AsSelf().Scoped();
        ctx.Container.Add(UserConfigFactory).AsSelf().Scoped();

        // serializers and http factories
        // market
        ctx.AddHttpRequestFactoryWithJsonSerializer(ExchangeInfoKey, Contracts.Market.ExchangeInfo);
        ctx.AddHttpRequestFactoryWithJsonSerializer(CandleKey, Contracts.Market.Candle);
        ctx.AddHttpRequestFactoryWithJsonSerializer(InstrumentTickerKey, Contracts.Market.InstrumentTicker);
        ctx.AddHttpRequestFactoryWithJsonSerializer(ServerTimeKey, Contracts.Shared.ServerTime);

        // user data load
        ctx.AddHttpRequestFactoryWithJsonSerializer(GetAccountKey, Contracts.User.GetAccount);
        ctx.AddHttpRequestFactoryWithJsonSerializer(GetOrderKey, Contracts.User.GetOrder);
        ctx.AddHttpRequestFactoryWithJsonSerializer(GetTradeKey, Contracts.User.GetTrade);

        // user data trade
        ctx.AddHttpRequestFactoryWithJsonSerializer(InitOrderKey, Contracts.User.InitOrder);
        ctx.AddHttpRequestFactoryWithJsonSerializer(ModifyOrderKey, Contracts.User.ModifyOrder);
        ctx.AddHttpRequestFactoryWithJsonSerializer(CancelOrderKey, Contracts.User.CancelOrder);
        ctx.AddHttpRequestFactoryWithJsonSerializer(CancelAllOrdersKey, Contracts.User.CancelAllOrders);

        // user data updates
        ctx.AddHttpRequestFactoryWithJsonSerializer(ListenKeyKey, Contracts.User.ListenKey);
        ctx.AddJsonSerializer(AccountUpdateKey, Contracts.User.AccountUpdate);
        ctx.AddJsonSerializer(OrderUpdateKey, Contracts.User.OrderUpdate);

        // services
        ctx.Container.Add(BookTickerServiceFactory).AsSelf().Scoped();
        ctx.Container.Add(SignatureServiceFactory).AsSelf().Scoped();
        ctx.Container.Add(ListenKeyResolverFactory).AsSelf().Scoped();
        ctx.Container.Add(UserStreamFactory).AsSelf().Scoped();

        return ctx;
    }

    private static IServerTimeProvider ServerTimeProviderFactory(IServiceProvider sp, object key)
    {
        var providerKey = key.CastTo<ProviderKey>();

        var requestFactory = sp.ResolveHttpRequestFactory(ServerTimeKey);
        var httpApi = Endpoints.GetHttpApi(providerKey.Environment);
        var providerConfig = sp.Resolve<ProviderConfiguration>();
        var logger = sp.Resolve<ILogger>();

        return new ServerTimeProvider(requestFactory, httpApi, "/api/v1/time", providerConfig.ServerTime, logger);
    }

    private static MarketConfig MarketConfigFactory(IServiceProvider sp)
    {
        var marketSettings = sp.Resolve<Injected<MarketSettings>>().Value;

        var httpApi = Endpoints.GetHttpApi(marketSettings.Environment);
        var wsApi = Endpoints.GetWsApi(marketSettings.Environment);

        return new MarketConfig
        {
            Provider = marketSettings.Provider,
            Environment = marketSettings.Environment,
            HttpApi = httpApi,
            WsApi = wsApi,
            WsUriPath = "/stream",
        };
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

    private static BookTickerService BookTickerServiceFactory(IServiceProvider sp)
    {
        var config = sp.Resolve<MarketConfig>();
        var serializer = sp.ResolveSerializer<ReadOnlyMemory<byte>>(
            InstrumentTickerKey,
            MediaTypeNames.Application.Json
        );
        var statusReporter = sp.Resolve<IStatusReporter>();
        var logger = sp.Resolve<ILogger>();

        return new BookTickerService(config, serializer, statusReporter, logger);
    }

    private static SignatureService SignatureServiceFactory(IServiceProvider sp)
    {
        var userSettings = sp.Resolve<Injected<UserSettings>>().Value;
        var serverTimeTracker = sp.Resolve<IServerTimeTracker>();

        return new SignatureService(userSettings, serverTimeTracker);
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
