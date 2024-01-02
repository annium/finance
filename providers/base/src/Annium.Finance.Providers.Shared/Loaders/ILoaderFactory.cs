using System;
using System.Threading;
using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Domain.Operations;

namespace Annium.Finance.Providers.Shared.Loaders;

public interface ILoaderFactory
{
    ISnapshotLoader<T> CreateSnapshotLoader<T>(
        SnapshotLoaderConfig cfg,
        Func<CancellationToken, Task<IBaseResult<T?>>> load
    );

    ICompositeLoader<T> CreateCompositeLoader<T>(
        CompositeLoaderConfig cfg,
        Func<CancellationToken, Task<IBaseResult<T?>>> load
    );

    IKeyedLoader<TKey, TContext, TData> CreateKeyedLoader<TKey, TContext, TData>(
        CompositeLoaderConfig cfg,
        TContext initialContext,
        Func<TKey, TContext, CancellationToken, Task<IBaseResult<TData?>>> getLoad,
        Func<TKey, TContext, TData, TContext> getContext
    )
        where TKey : notnull;
}
