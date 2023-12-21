using System;
using System.Threading;
using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Domain.Operations;
using Annium.Finance.Providers.Shared.Internal.Services;

namespace Annium.Finance.Providers.Shared.Services;

public interface ILoaderFactory
{
    ISnapshotLoader<T> CreateSnapshotLoader<T>(
        SnapshotLoaderConfig cfg,
        Func<CancellationToken, Task<IBaseResult<T>>> load
    );

    ICompositeLoader<T> CreateCompositeLoader<T>(
        SnapshotLoaderConfig cfg,
        Func<CancellationToken, Task<IBaseResult<T>>> load,
        int intervalPeriod,
        int debouncePeriod
    );

    IKeyedLoader<TKey, TContext, TData> CreateKeyedLoader<TKey, TContext, TData>(
        TContext initialContext,
        SnapshotLoaderConfig cfg,
        Func<TKey, TContext, CancellationToken, Task<IBaseResult<TData>>> getLoad,
        Func<TKey, TContext, TData, TContext> getContext,
        int intervalPeriod,
        int debouncePeriod
    )
        where TKey : notnull;
}
