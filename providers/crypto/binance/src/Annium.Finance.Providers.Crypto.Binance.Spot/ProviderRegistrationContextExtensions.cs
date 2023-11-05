using Annium.Finance.Providers.Abstractions.Domain.Enums;
using Annium.Finance.Providers.Crypto.Binance.Spot.Internal.Connectors;
using Annium.Finance.Providers.Crypto.Binance.Spot.Internal.Contracts;
using Annium.Finance.Providers.Crypto.Binance.Spot.Internal.Services;
using Annium.Finance.Providers.Shared;
using Annium.Finance.Providers.Shared.Internal;
using static Annium.Finance.Providers.Crypto.Binance.Spot.Constants;

namespace Annium.Finance.Providers.Crypto.Binance.Spot;

public static class ProviderRegistrationContextExtensions
{
    public static ProviderRegistrationContext WithBinanceSpot(this ProviderRegistrationContext ctx)
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
