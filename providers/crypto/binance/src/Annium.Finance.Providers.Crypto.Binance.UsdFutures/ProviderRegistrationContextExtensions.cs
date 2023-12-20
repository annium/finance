using System;
using System.Net.Mime;
using Annium.Core.DependencyInjection;
using Annium.Finance.Providers.Abstractions.Domain.Enums;
using Annium.Finance.Providers.Abstractions.Domain.Models;
using Annium.Finance.Providers.Crypto.Binance.Base.Services;
using Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal;
using Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Connectors;
using Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Contracts;
using Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Services;
using Annium.Finance.Providers.Shared;
using Annium.Finance.Providers.Shared.Connectors;
using Annium.Logging;
using Annium.Serialization.Abstractions;
using static Annium.Finance.Providers.Crypto.Binance.UsdFutures.Constants;
using MarketConfig = Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.MarketConfig;
using UserConfig = Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.UserConfig;

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
        ctx.AddProvider<MarketProvider, MarketConnector, UserProvider, QueryProcessor, UserConnector, FinanceService>(
            Provider,
            ProviderEnvironment.Real | ProviderEnvironment.Test
        );

        // settings
        ctx.Container.Add<ProviderConfiguration>().AsSelf().Singleton();
        ctx.Container.Add(MarketConfigFactory).AsSelf().Scoped();
        ctx.Container.Add(UserConfigFactory).AsSelf().Scoped();

        // serializers and http factories
        // market
        ctx.AddHttpRequestFactoryWithJsonSerializer(ExchangeInfoKey, Contracts.Market.ExchangeInfo);
        ctx.AddHttpRequestFactoryWithJsonSerializer(CandleKey, Contracts.Market.Candle);
        ctx.AddHttpRequestFactoryWithJsonSerializer(InstrumentTickerKey, Contracts.Market.InstrumentTicker);
        ctx.AddHttpRequestFactoryWithJsonSerializer(ServerTimeKey, Contracts.Shared.ServerTime);

        // user data load
        ctx.AddHttpRequestFactoryWithJsonSerializer(GetAccount, Contracts.User.GetAccount);
        ctx.AddHttpRequestFactoryWithJsonSerializer(GetOrder, Contracts.User.GetOrder);
        ctx.AddHttpRequestFactoryWithJsonSerializer(GetTrade, Contracts.User.GetTrade);

        // user data trade
        ctx.AddHttpRequestFactoryWithJsonSerializer(InitOrderKey, Contracts.User.InitOrder);
        ctx.AddHttpRequestFactoryWithJsonSerializer(ModifyOrderKey, Contracts.User.ModifyOrder);
        ctx.AddHttpRequestFactoryWithJsonSerializer(CancelOrderKey, Contracts.User.CancelOrder);
        ctx.AddHttpRequestFactoryWithJsonSerializer(CancelAllOrdersKey, Contracts.User.CancelAllOrders);

        // user data updates
        ctx.AddHttpRequestFactoryWithJsonSerializer(ListenKeyKey, Contracts.User.ListenKey);
        ctx.AddHttpRequestFactoryWithJsonSerializer(
            AccountConfigurationUpdateKey,
            Contracts.User.AccountConfigurationUpdate
        );
        ctx.AddHttpRequestFactoryWithJsonSerializer(
            BalanceAndPositionUpdateKey,
            Contracts.User.BalanceAndPositionUpdate
        );
        ctx.AddHttpRequestFactoryWithJsonSerializer(OrderUpdateKey, Contracts.User.OrderUpdate);

        // services
        ctx.Container.Add(BookTickerServiceFactory).AsSelf().Scoped();
        ctx.Container.Add(SignatureServiceFactory).AsSelf().Scoped();

        return ctx;
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
            WsMarketEndpoint = "/stream",
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
            ListenKeyBase = "/ws/",
            ListenKeyFetchInterval = providerConfig.ListenKeyFetchInterval,
            ListenKeyConfirmInterval = providerConfig.ListenKeyConfirmInterval,
            ReloadAccountInterval = providerConfig.ReloadAccountInterval,
            ReloadAccountDebounce = providerConfig.ReloadAccountDebounce,
            ReloadOrdersInterval = providerConfig.ReloadOrdersInterval,
            ReloadOrdersDebounce = providerConfig.ReloadOrdersDebounce,
            ReloadDealsDebounce = providerConfig.ReloadDealsDebounce,
        };
    }

    private static BookTickerService BookTickerServiceFactory(IServiceProvider sp)
    {
        var config = sp.Resolve<MarketConfig>();
        var serializerKey = SerializerKey.Create(InstrumentTickerKey, MediaTypeNames.Application.Json);
        var serializer = sp.ResolveKeyed<ISerializer<ReadOnlyMemory<byte>>>(serializerKey);
        var statusReporter = sp.Resolve<IStatusReporter>();
        var logger = sp.Resolve<ILogger>();

        return new BookTickerService(config, serializer, statusReporter, logger);
    }

    private static SignatureService SignatureServiceFactory(IServiceProvider sp)
    {
        var config = sp.Resolve<UserConfig>();

        return new SignatureService(config.Key, config.Secret);
    }
}
