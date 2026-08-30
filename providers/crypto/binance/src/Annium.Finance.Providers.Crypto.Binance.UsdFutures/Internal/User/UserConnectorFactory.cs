using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using Annium.Core.DependencyInjection;
using Annium.Core.Mapper;
using Annium.Finance.Providers.Abstractions.Connectors.User;
using Annium.Finance.Providers.Abstractions.Domain.Shared;
using Annium.Finance.Providers.Abstractions.Domain.Shared.Operations;
using Annium.Finance.Providers.Abstractions.Domain.User;
using Annium.Finance.Providers.Core.Shared;
using Annium.Finance.Providers.Core.Shared.RateLimits;
using Annium.Finance.Providers.Core.Shared.Status;
using Annium.Finance.Providers.Core.User;
using Annium.Finance.Providers.Crypto.Binance.Base.User;
using Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.User.Services;
using Annium.Logging;
using Annium.Net.Http;
using Annium.Serialization.Abstractions;
using static Annium.Finance.Providers.Crypto.Binance.UsdFutures.Constants;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.User;

/// <summary>
/// Builds fully-wired <see cref="UserConnector"/> instances for the USD-M futures provider: resolves
/// configuration, the underlying provider, request signing, order management request factories, the listen key
/// resolver and user data stream, and the context/orders/trades loaders.
/// </summary>
/// <param name="sp">The service provider used to resolve dependencies.</param>
internal class UserConnectorFactory(IServiceProvider sp) : IUserConnectorInstanceFactory
{
    /// <summary>
    /// Creates a user connector for the given settings.
    /// </summary>
    /// <param name="settings">The user settings identifying the provider, environment and credentials.</param>
    /// <param name="disposable">Accumulates cleanup actions for the connector's lifetime.</param>
    /// <returns>A new, ready-to-use user connector.</returns>
    public IUserConnector Create(UserSettings settings, AsyncDisposableBox disposable)
    {
        var config = sp.Resolve<IMapper>().Map<UserConfig>(settings);
        var providerKey = settings.GetProviderKey();

        var timeProvider = sp.Resolve<ITimeProvider>();
        var provider = sp.CreateUserProvider(settings);
        var queryProcessor = sp.Resolve<QueryProcessor>();
        var signatureService = sp.CreateSignatureService(settings, providerKey);
        var setLeverageRequestFactory = sp.ResolveHttpRequestFactory(SetLeverageKey);
        var initOrderRequestFactory = sp.ResolveHttpRequestFactory(InitOrderKey);
        var modifyOrderRequestFactory = sp.ResolveHttpRequestFactory(ModifyOrderKey);
        var cancelOrderRequestFactory = sp.ResolveHttpRequestFactory(CancelOrderKey);
        var cancelAllOrdersRequestFactory = sp.ResolveHttpRequestFactory(CancelAllOrdersKey);
        var listenKeyResolver = sp.CreateListenKeyResolver(
            config,
            "/fapi/v1/listenKey",
            ListenKeyKey,
            signatureService
        );
        var userStream = sp.CreateUserStream(config, listenKeyResolver);
        var orderUpdateEventSerializer = sp.ResolveSerializer<ReadOnlyMemory<byte>>(
            OrderUpdateKey,
            MediaTypeNames.Application.Json
        );
        var contextLoder = sp.CreateUserContextLoader(config.ReloadContext, provider, ref disposable);
        var ordersLoader = sp.CreateCompositeLoader(config.ReloadOrders, LoadOrdersAsync);
        var tradesLoader = sp.CreateKeyedLoader<string, long, IReadOnlyCollection<TradeModel>>(
            config.ReloadTrades,
            timeProvider.Now.ToUnixTimeMilliseconds(),
            LoadTradesAsync,
            GetTradesContext
        );

        var rateLimiter = sp.Resolve<IRateLimiter>();
        var reporter = sp.Resolve<IStatusReporter>();
        var monitor = sp.Resolve<IStatusMonitor>();
        var logger = sp.Resolve<ILogger>();

        return new UserConnector(
            config,
            provider,
            queryProcessor,
            signatureService,
            setLeverageRequestFactory,
            initOrderRequestFactory,
            modifyOrderRequestFactory,
            cancelOrderRequestFactory,
            cancelAllOrdersRequestFactory,
            rateLimiter,
            contextLoder,
            ordersLoader,
            tradesLoader,
            userStream,
            orderUpdateEventSerializer,
            reporter,
            monitor,
            disposable,
            logger
        );

        async Task<IBaseResult<IReadOnlyCollection<OrderModel>?>> LoadOrdersAsync(CancellationToken ct)
        {
            var result = await provider.LoadOpenOrdersAsync();

            return result;
        }
        async Task<IBaseResult<IReadOnlyCollection<TradeModel>?>> LoadTradesAsync(
            string symbol,
            long since,
            CancellationToken ct
        )
        {
            var result = await provider.LoadTradesAsync(symbol, since);

            return result;
        }

        long GetTradesContext(string symbol, long since, IReadOnlyCollection<TradeModel> trades)
        {
            var result = trades.Select(x => x.Moment).MaxBy(x => x);

            return result;
        }
    }
}
