using System;
using System.Threading;
using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Domain.Operations;
using Annium.Finance.Providers.Shared.Connectors;
using Annium.Logging;

namespace Annium.Finance.Providers.Shared.Internal.Services;

internal sealed class KeyedLoaderEntry<TKey, TContext, TData>
    where TKey : notnull
{
    public TKey Key { get; }
    public TContext Context { get; private set; }
    public CompositeLoader<TData> Loader { get; }
    private readonly Func<TKey, TContext, CancellationToken, Task<IBaseResult<TData>>> _getLoad;

    public KeyedLoaderEntry(
        TKey key,
        TContext context,
        SnapshotLoaderConfig loaderConfig,
        Func<TKey, TContext, CancellationToken, Task<IBaseResult<TData>>> getLoad,
        IStatusReporter statusReporter,
        ILogger logger,
        int intervalPeriod,
        int debouncePeriod
    )
    {
        Key = key;
        Context = context;
        _getLoad = getLoad;
        var snapshotLoader = new SnapshotLoader<TData>(loaderConfig, GetLoad, statusReporter, logger);
        Loader = new CompositeLoader<TData>(snapshotLoader, intervalPeriod, debouncePeriod, logger);
    }

    public void UpdateContext(TContext context)
    {
        Context = context;
    }

    private Task<IBaseResult<TData>> GetLoad(CancellationToken ct)
    {
        return _getLoad(Key, Context, ct);
    }
}
