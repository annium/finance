using Annium.Finance.Providers.Abstractions.Domain.Enums;
using Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Connectors;
using Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Contracts;
using Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Services;
using Annium.Finance.Providers.Shared;
using static Annium.Finance.Providers.Crypto.Binance.UsdFutures.Constants;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures;

public static class ProviderRegistrationContextExtensions
{
    public static ProviderRegistrationContext WithBinanceUsdFutures(this ProviderRegistrationContext ctx)
    {
        // provider
        ctx.AddProvider<MarketProvider, MarketConnector, UserProvider, UserConnector, FinanceService>(
            Provider,
            ProviderEnvironment.Real | ProviderEnvironment.Test
        );

        // provider-specific components
        ctx.AddHttpRequestFactoryWithJsonSerializer(ExchangeInfoKey, Contracts.Market.ExchangeInfo);
        ctx.AddHttpRequestFactoryWithJsonSerializer(CandleKey, Contracts.Market.Candle);
        ctx.AddHttpRequestFactoryWithJsonSerializer(InstrumentTickerKey, Contracts.Market.InstrumentTicker);

        return ctx;
    }
}
