using System;
using System.Threading;
using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Connectors.Shared;
using Annium.Finance.Providers.Abstractions.Domain.Shared.Operations;
using Annium.Finance.Providers.Core.Shared.Loaders;
using Annium.Finance.Providers.Core.Shared.Status;
using Annium.Logging;

namespace Annium.Finance.Providers.Core.Internal.Shared.Loaders;

/// <summary>
/// The per-key state managed by a <see cref="KeyedLoader{TKey, TContext, TData}"/>: a key, its current context,
/// and a private <see cref="CompositeLoader{T}"/> (backed by a <see cref="SnapshotLoader{T}"/> that always
/// reports <see cref="ConnectorStatus.Connected"/>) that performs its loads.
/// </summary>
/// <typeparam name="TKey">The type of key identifying this entry.</typeparam>
/// <typeparam name="TContext">The type of context passed to and updated by loads.</typeparam>
/// <typeparam name="TData">The type of data loaded.</typeparam>
internal sealed class KeyedLoaderEntry<TKey, TContext, TData> : IDisposable
    where TKey : notnull
{
    /// <summary>Gets the key identifying this entry.</summary>
    public TKey Key { get; }

    /// <summary>Gets the entry's current context, as of the last successful load.</summary>
    public TContext Context { get; private set; }

    /// <summary>Raised with the loaded data every time this entry's load succeeds.</summary>
    public event Action<TData> OnData = delegate { };

    /// <summary>The underlying composite loader that drives this entry's loads.</summary>
    private readonly ICompositeLoader<TData> _loader;

    /// <summary>The delegate that performs a single load for this entry's key and context.</summary>
    private readonly Func<TKey, TContext, CancellationToken, Task<IBaseResult<TData?>>> _getLoad;

    /// <summary>
    /// Initializes a new instance of the <see cref="KeyedLoaderEntry{TKey, TContext, TData}"/> class, creating
    /// its underlying snapshot and composite loaders.
    /// </summary>
    /// <param name="key">The key identifying this entry.</param>
    /// <param name="context">The initial context for this entry.</param>
    /// <param name="config">The timing configuration for the underlying loader.</param>
    /// <param name="getLoad">The delegate that performs a single load for this entry's key and context.</param>
    /// <param name="statusReporter">The status reporter the underlying snapshot loader binds its connection status to.</param>
    /// <param name="logger">The logger instance.</param>
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

    /// <summary>
    /// Disposes the underlying composite loader.
    /// </summary>
    public void Dispose()
    {
        _loader.OnData -= HandleData;
        _loader.Dispose();
    }

    /// <summary>
    /// Starts the underlying composite loader, without reporting status.
    /// </summary>
    public void Start()
    {
        _loader.Start(false);
    }

    /// <summary>
    /// Requests a reload of this entry via the underlying composite loader's debounce timer.
    /// </summary>
    public void Request()
    {
        _loader.Request();
    }

    /// <summary>
    /// Replaces this entry's <see cref="Context"/>, e.g. with the value derived from its last successful load.
    /// </summary>
    /// <param name="context">The new context.</param>
    public void UpdateContext(TContext context)
    {
        Context = context;
    }

    /// <summary>
    /// Invokes <see cref="_getLoad"/> with this entry's current key and context.
    /// </summary>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A result containing the loaded data.</returns>
    private Task<IBaseResult<TData?>> GetLoadAsync(CancellationToken ct)
    {
        return _getLoad(Key, Context, ct);
    }

    /// <summary>Forwards loaded data from the underlying composite loader through <see cref="OnData"/>.</summary>
    /// <param name="data">The loaded data.</param>
    private void HandleData(TData data) => OnData(data);
}
