using System;
using System.Threading;
using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Connectors.Connectors;
using Annium.Finance.Providers.Abstractions.Domain.Operations;
using Annium.Finance.Providers.Shared.Connectors;
using Annium.Finance.Providers.Shared.Loaders;
using Annium.Logging;

namespace Annium.Finance.Providers.Shared.Internal.Loaders;

internal sealed class KeyedLoaderEntry<TKey, TContext, TData>
    where TKey : notnull
{
    public TKey Key { get; }
    public TContext Context { get; private set; }
    public CompositeLoader<TData> Loader { get; }
    private readonly Func<TKey, TContext, CancellationToken, Task<IBaseResult<TData?>>> _getLoad;

    public KeyedLoaderEntry(
        TKey key,
        TContext context,
        CompositeLoaderConfig config,
        Func<TKey, TContext, CancellationToken, Task<IBaseResult<TData?>>> getLoad,
        IStatusReporter statusReporter,
        ILogger logger
    )
    {
        Key = key;
        Context = context;
        _getLoad = getLoad;
        var snapshotLoader = new SnapshotLoader<TData>(
            config,
            GetLoadAsync,
            statusReporter,
            ConnectorStatus.Connected,
            logger
        );
        Loader = new CompositeLoader<TData>(snapshotLoader, config.Interval, config.Debounce, logger);
    }

    public void UpdateContext(TContext context)
    {
        Context = context;
    }

    private Task<IBaseResult<TData?>> GetLoadAsync(CancellationToken ct)
    {
        return _getLoad(Key, Context, ct);
    }
}
