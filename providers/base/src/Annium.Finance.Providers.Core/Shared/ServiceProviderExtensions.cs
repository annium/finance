using System;
using System.Threading;
using System.Threading.Tasks;
using Annium.Core.DependencyInjection;
using Annium.Finance.Providers.Abstractions.Connectors.Shared;
using Annium.Finance.Providers.Abstractions.Domain.Shared.Operations;
using Annium.Finance.Providers.Core.Internal.Shared.Loaders;
using Annium.Finance.Providers.Core.Internal.Shared.RateLimits;
using Annium.Finance.Providers.Core.Shared.Loaders;
using Annium.Finance.Providers.Core.Shared.RateLimits;
using Annium.Finance.Providers.Core.Shared.Status;
using Annium.Logging;

namespace Annium.Finance.Providers.Core.Shared;

public static class ServiceProviderExtensions
{
    public static ISnapshotLoader<T> CreateSnapshotLoader<T>(
        this IServiceProvider sp,
        SnapshotLoaderConfig cfg,
        Func<CancellationToken, Task<IBaseResult<T?>>> load
    )
    {
        var statusReporter = sp.Resolve<IStatusReporter>();
        var logger = sp.Resolve<ILogger>();

        return new SnapshotLoader<T>(cfg, load, statusReporter, ConnectorStatus.Disconnected, logger);
    }

    public static ICompositeLoader<T> CreateCompositeLoader<T>(
        this IServiceProvider sp,
        CompositeLoaderConfig cfg,
        Func<CancellationToken, Task<IBaseResult<T?>>> load
    )
    {
        var loader = sp.CreateSnapshotLoader(cfg, load);
        var logger = sp.Resolve<ILogger>();

        return new CompositeLoader<T>(loader, cfg.Interval, cfg.Debounce, logger);
    }

    public static IKeyedLoader<TKey, TContext, TData> CreateKeyedLoader<TKey, TContext, TData>(
        this IServiceProvider sp,
        CompositeLoaderConfig cfg,
        TContext initialContext,
        Func<TKey, TContext, CancellationToken, Task<IBaseResult<TData?>>> getLoad,
        Func<TKey, TContext, TData, TContext> getContext
    )
        where TKey : notnull
    {
        var logger = sp.Resolve<ILogger>();
        return new KeyedLoader<TKey, TContext, TData>(sp, cfg, initialContext, getLoad, getContext, logger);
    }

    public static IRateLimiter CreateRateLimiter(
        this IServiceProvider sp,
        int limit,
        int lowerWeightValue,
        int lowerWeightDelay
    )
    {
        var logger = sp.Resolve<ILogger>();

        return new RateLimiter(limit, lowerWeightValue, lowerWeightDelay, logger);
    }
}
