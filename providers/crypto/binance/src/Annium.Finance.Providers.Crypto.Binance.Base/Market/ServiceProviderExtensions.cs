using System;
using System.Net.Mime;
using Annium.Core.DependencyInjection;
using Annium.Finance.Providers.Core.Shared.Status;
using Annium.Finance.Providers.Crypto.Binance.Base.Internal.Market.Services;
using Annium.Finance.Providers.Crypto.Binance.Base.Market.Services;
using Annium.Logging;
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
        var serializer = sp.ResolveSerializer<ReadOnlyMemory<byte>>(
            instrumentTickerKey,
            MediaTypeNames.Application.Json
        );
        var statusReporter = sp.Resolve<IStatusReporter>();
        var logger = sp.Resolve<ILogger>();

        var bookTickerService = new BookTickerService(config, serializer, statusReporter, logger);

        disposable += bookTickerService;

        return bookTickerService;
    }
}
