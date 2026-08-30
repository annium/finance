using System;
using System.Net.Mime;
using Annium.Core.DependencyInjection;
using Annium.Finance.Providers.Core.Shared.Status;
using Annium.Finance.Providers.Crypto.Binance.Base.Internal.Market.Services;
using Annium.Finance.Providers.Crypto.Binance.Base.Market.Services;
using Annium.Logging;
using Annium.Serialization.Abstractions;

namespace Annium.Finance.Providers.Crypto.Binance.Base.Market;

/// <summary>Factory extension methods for constructing Binance market-data services from an <see cref="IServiceProvider"/>.</summary>
public static class ServiceProviderExtensions
{
    /// <summary>Creates a <see cref="BookTickerService"/> wired to the given market configuration and registers it for disposal in the given box.</summary>
    /// <param name="sp">The service provider to resolve dependencies from.</param>
    /// <param name="config">The market configuration providing the WebSocket API endpoint.</param>
    /// <param name="instrumentTickerKey">The keyed serializer registration key to resolve the instrument ticker serializer with.</param>
    /// <param name="disposable">The disposable box the created service is registered into for teardown.</param>
    /// <returns>The created book ticker service.</returns>
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
