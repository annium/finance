using System;
using System.Threading;
using System.Threading.Tasks;
using Annium.Core.DependencyInjection;
using Annium.Finance.Providers.Abstractions.Domain.Operations;
using Annium.Finance.Providers.Shared.Connectors;
using Annium.Finance.Providers.Shared.Loaders;
using Annium.Logging;

namespace Annium.Finance.Providers.Shared.Internal.Loaders;

internal class LoaderFactory : ILoaderFactory
{
    private readonly IServiceProvider _sp;
    private readonly ILogger _logger;

    public LoaderFactory(IServiceProvider sp, ILogger logger)
    {
        _sp = sp;
        _logger = logger;
    }

    public ISnapshotLoader<T> CreateSnapshotLoader<T>(
        SnapshotLoaderConfig cfg,
        Func<CancellationToken, Task<IBaseResult<T?>>> load
    )
    {
        var reporter = _sp.Resolve<IStatusReporter>();

        return new SnapshotLoader<T>(cfg, load, reporter, _logger);
    }

    public ICompositeLoader<T> CreateCompositeLoader<T>(
        CompositeLoaderConfig cfg,
        Func<CancellationToken, Task<IBaseResult<T?>>> load
    )
    {
        var loader = CreateSnapshotLoader(cfg, load);

        return new CompositeLoader<T>(loader, cfg.Interval, cfg.Debounce, _logger);
    }

    public IKeyedLoader<TKey, TContext, TData> CreateKeyedLoader<TKey, TContext, TData>(
        CompositeLoaderConfig cfg,
        TContext initialContext,
        Func<TKey, TContext, CancellationToken, Task<IBaseResult<TData?>>> getLoad,
        Func<TKey, TContext, TData, TContext> getContext
    )
        where TKey : notnull
    {
        return new KeyedLoader<TKey, TContext, TData>(_sp, cfg, initialContext, getLoad, getContext, _logger);
    }
}
