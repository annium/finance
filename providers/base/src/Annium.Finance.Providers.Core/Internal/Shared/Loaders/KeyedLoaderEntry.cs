using System;
using System.Threading;
using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Connectors.Shared;
using Annium.Finance.Providers.Abstractions.Domain.Shared.Operations;
using Annium.Finance.Providers.Core.Shared.Loaders;
using Annium.Finance.Providers.Core.Shared.Status;
using Annium.Logging;

namespace Annium.Finance.Providers.Core.Internal.Shared.Loaders;

internal sealed class KeyedLoaderEntry<TKey, TContext, TData> : IDisposable
    where TKey : notnull
{
    public TKey Key { get; }
    public TContext Context { get; private set; }
    public event Action<TData> OnData = delegate { };

    private readonly ICompositeLoader<TData> _loader;
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
        _loader = new CompositeLoader<TData>(snapshotLoader, config.Interval, config.Debounce, logger);
        _loader.OnData += HandleData;
    }

    public void Dispose()
    {
        _loader.OnData -= HandleData;
        _loader.Dispose();
    }

    public void Start()
    {
        _loader.Start(false);
    }

    public void Request()
    {
        _loader.Request();
    }

    public void UpdateContext(TContext context)
    {
        Context = context;
    }

    private Task<IBaseResult<TData?>> GetLoadAsync(CancellationToken ct)
    {
        return _getLoad(Key, Context, ct);
    }

    private void HandleData(TData data) => OnData(data);
}
