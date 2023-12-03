using Annium.Core.DependencyInjection;
using Annium.Finance.Providers.Abstractions.Domain.Enums;
using Annium.Finance.Providers.Abstractions.Domain.Interfaces;
using Annium.Finance.Providers.Abstractions.Domain.Models;
using Annium.Finance.Providers.Crypto.Binance.Spot.Internal;
using Annium.Finance.Providers.Crypto.Binance.Spot.Internal.Connectors;
using Annium.Finance.Providers.Crypto.Binance.Spot.Internal.Contracts;
using Annium.Finance.Providers.Crypto.Binance.Spot.Internal.Services;
using Annium.Finance.Providers.Shared;
using static Annium.Finance.Providers.Crypto.Binance.Spot.Constants;

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
            Provider,
            ProviderEnvironment.Real | ProviderEnvironment.Test
        );

        // settings
        ctx.Container
            .Add(sp =>
            {
                var marketConfig = sp.Resolve<Injected<IMarketConfig>>().Value;

                var httpApi = Endpoints.GetHttpApi(marketConfig.Environment);
                var wsApi = Endpoints.GetWsApi(marketConfig.Environment);

                return new Configuration
                {
                    Provider = marketConfig.Provider,
                    Environment = marketConfig.Environment,
                    HttpApi = httpApi,
                    WsApi = wsApi,
                    WsMarketEndpoint = "/stream",
                    ReloadAccountInterval = cfg.ReloadAccountInterval,
                    ReloadAccountDebounce = cfg.ReloadAccountDebounce,
                    ReloadOrdersInterval = cfg.ReloadOrdersInterval,
                    ReloadOrdersDebounce = cfg.ReloadOrdersDebounce,
                    ReloadDealsDebounce = cfg.ReloadDealsDebounce,
                };
            })
            .AsSelf()
            .Scoped();

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
        ctx.AddHttpRequestFactoryWithJsonSerializer(AccountUpdateKey, Contracts.User.AccountUpdate);
        ctx.AddHttpRequestFactoryWithJsonSerializer(OrderUpdateKey, Contracts.User.OrderUpdate);

        // services
        ctx.Container.Add<BookTickerService>().AsSelf().Scoped();

        return ctx;
    }
}
