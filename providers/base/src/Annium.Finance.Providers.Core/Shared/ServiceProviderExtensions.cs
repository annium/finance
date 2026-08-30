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

/// <summary>
/// Factory extension methods for creating loaders and rate limiters resolved through DI, for use by provider
/// implementations that don't need any of these types registered in the container directly.
/// </summary>
public static class ServiceProviderExtensions
{
    /// <summary>
    /// Creates a snapshot loader, resolving its status reporter and logger from the container. The loader's
    /// connection status starts out disconnected.
    /// </summary>
    /// <typeparam name="T">The type of data loaded.</typeparam>
    /// <param name="sp">The service provider to resolve dependencies from.</param>
    /// <param name="cfg">The timing configuration for fetch retries.</param>
    /// <param name="load">The delegate that performs a single fetch.</param>
    /// <returns>A new snapshot loader.</returns>
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

    /// <summary>
    /// Creates a composite loader wrapping a new snapshot loader created via <see cref="CreateSnapshotLoader{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of data loaded.</typeparam>
    /// <param name="sp">The service provider to resolve dependencies from.</param>
    /// <param name="cfg">The timing configuration for fetch retries, interval reloads, and debounced requests.</param>
    /// <param name="load">The delegate that performs a single fetch.</param>
    /// <returns>A new composite loader.</returns>
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

    /// <summary>
    /// Creates a keyed loader, resolving its logger from the container. The service provider is also captured
    /// by the returned loader, to resolve a status reporter for each new per-key entry it creates.
    /// </summary>
    /// <typeparam name="TKey">The type of key identifying each independent load.</typeparam>
    /// <typeparam name="TContext">The type of per-key context passed to and updated by loads.</typeparam>
    /// <typeparam name="TData">The type of data loaded.</typeparam>
    /// <param name="sp">The service provider to resolve dependencies from.</param>
    /// <param name="cfg">The timing configuration passed to every entry's underlying loader.</param>
    /// <param name="initialContext">The context assigned to every newly created entry before its first load.</param>
    /// <param name="getLoad">The delegate that performs a single load for a key/context pair.</param>
    /// <param name="getContext">The delegate that derives an entry's updated context from its key, prior context, and loaded data.</param>
    /// <returns>A new keyed loader.</returns>
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

    /// <summary>
    /// Creates a rate limiter, resolving its logger from the container.
    /// </summary>
    /// <param name="sp">The service provider to resolve dependencies from.</param>
    /// <param name="limit">The initial rate limit.</param>
    /// <param name="lowerWeightValue">The amount by which used weight is decayed on each tick once it crosses the water mark.</param>
    /// <param name="lowerWeightDelay">The delay, in milliseconds, between decay ticks.</param>
    /// <returns>A new rate limiter.</returns>
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
