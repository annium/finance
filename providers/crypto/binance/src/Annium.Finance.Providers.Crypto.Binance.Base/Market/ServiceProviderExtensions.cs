using System;
using System.Net.Mime;
using Annium.Core.DependencyInjection;
using Annium.Finance.Providers.Crypto.Binance.Base.Market.Services;
using Annium.Serialization.Abstractions;

namespace Annium.Finance.Providers.Crypto.Binance.Base.Market;

public static class ServiceProviderExtensions
{
    public static IBookTickerService CreateBookTickerService(
        this IServiceProvider sp,
        MarketConfigBase config,
        string instrumentTickerKey,
        ref AsyncDisposableBox disposable
    )
    {
        var bookTickerServiceFactory = sp.Resolve<IBookTickerServiceFactory>();
        var bookTickerService = bookTickerServiceFactory.Create(
            config,
            SerializerKey.Create(instrumentTickerKey, MediaTypeNames.Application.Json)
        );
        disposable += bookTickerService;

        return bookTickerService;
    }
}
