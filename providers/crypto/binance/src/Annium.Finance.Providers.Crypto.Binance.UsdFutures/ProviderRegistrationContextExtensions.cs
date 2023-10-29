using System.Text.Json.Serialization;
using Annium.Core.DependencyInjection;
using Annium.Finance.Providers.Abstractions.Domain.Enums;
using Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Connectors;
using Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Services;
using Annium.Finance.Providers.Shared;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures;

public static class ProviderRegistrationContextExtensions
{
    public static ProviderRegistrationContext WithBinanceUsdFutures(this ProviderRegistrationContext ctx)
    {
        // provider
        ctx.AddProvider<MarketProvider, MarketConnector, UserProvider, UserConnector, FinanceService>(
            Constants.Provider,
            ProviderEnvironment.Real | ProviderEnvironment.Test
        );

        // provider-specific components
        ctx.Container
            .AddSerializers(Constants.Provider)
            .WithJson(opts =>
            {
                // TODO: explicit converters list
                opts.UseCamelCaseNamingPolicy();
                opts.NumberHandling = JsonNumberHandling.AllowReadingFromString;
                opts.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            });
        ctx.Container.AddHttpRequestFactory(Constants.Provider);

        return ctx;
    }
}
