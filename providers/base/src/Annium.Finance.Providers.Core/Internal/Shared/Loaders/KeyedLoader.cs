using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Annium.Core.DependencyInjection;
using Annium.Finance.Providers.Abstractions.Domain.Shared.Operations;
using Annium.Finance.Providers.Core.Shared.Loaders;
using Annium.Finance.Providers.Core.Shared.Status;
using Annium.Logging;

namespace Annium.Finance.Providers.Core.Internal.Shared.Loaders;

/// <summary>
/// Default <see cref="IKeyedLoader{TKey, TContext, TData}"/> implementation. Lazily creates and starts one
/// <see cref="KeyedLoaderEntry{TKey, TContext, TData}"/> per key on first <see cref="Request"/>, and keeps every
/// entry's context up to date as its loads succeed. Entries, once created, live for the lifetime of the loader.
/// </summary>
/// <typeparam name="TKey">The type of key identifying each independent load.</typeparam>
/// <typeparam name="TContext">The type of per-key context passed to and updated by loads.</typeparam>
/// <typeparam name="TData">The type of data loaded.</typeparam>
internal sealed class KeyedLoader<TKey, TContext, TData> : IKeyedLoader<TKey, TContext, TData>, ILogSubject
    where TKey : notnull
{
    /// <summary>Gets the logger instance.</summary>
    public ILogger Logger { get; }

    /// <summary>Raised with the key, its (pre-update) context, and the loaded data every time a load succeeds.</summary>
    public event Action<TKey, TContext, TData> OnData = delegate { };

    /// <summary>The service provider used to resolve an <see cref="IStatusReporter"/> for each new entry.</summary>
    private readonly IServiceProvider _sp;

    /// <summary>The timing configuration passed to every entry's underlying loader.</summary>
    private readonly CompositeLoaderConfig _config;

    /// <summary>The context assigned to every newly created entry before its first load.</summary>
    private readonly TContext _initialContext;

    /// <summary>The delegate that performs a single load for a key/context pair.</summary>
    private readonly Func<TKey, TContext, CancellationToken, Task<IBaseResult<TData?>>> _getLoad;

    /// <summary>The delegate that derives an entry's updated context from its key, prior context, and loaded data.</summary>
    private readonly Func<TKey, TContext, TData, TContext> _getContext;

    /// <summary>
    /// Guards <see cref="_entries"/> and <see cref="_isDisposed"/> together. Creating an entry is not free -
    /// it resolves a status reporter, binds it, and starts the entry, meaning a network fetch and a pair of
    /// timers - and an entry that escapes this dictionary is unreachable and undisposable: its timers keep
    /// firing and its reporter stays bound for the life of the process. Two callers racing for a key it does
    /// not hold, and a caller racing disposal, are both that same escape.
    /// </summary>
    private readonly Lock _locker = new();

    /// <summary>The loader entries created so far, keyed by <typeparamref name="TKey"/>.</summary>
    private readonly Dictionary<TKey, KeyedLoaderEntry<TKey, TContext, TData>> _entries = new();

    /// <summary>Whether this loader has been disposed, after which no new entry may be created.</summary>
    private bool _isDisposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="KeyedLoader{TKey, TContext, TData}"/> class.
    /// </summary>
    /// <param name="sp">The service provider used to resolve an <see cref="IStatusReporter"/> for each new entry.</param>
    /// <param name="config">The timing configuration passed to every entry's underlying loader.</param>
    /// <param name="initialContext">The context assigned to every newly created entry before its first load.</param>
    /// <param name="getLoad">The delegate that performs a single load for a key/context pair.</param>
    /// <param name="getContext">The delegate that derives an entry's updated context from its key, prior context, and loaded data.</param>
    /// <param name="logger">The logger instance.</param>
    public KeyedLoader(
        IServiceProvider sp,
        CompositeLoaderConfig config,
        TContext initialContext,
        Func<TKey, TContext, CancellationToken, Task<IBaseResult<TData?>>> getLoad,
        Func<TKey, TContext, TData, TContext> getContext,
        ILogger logger
    )
    {
        Logger = logger;
        _sp = sp;
        _config = config;
        _initialContext = initialContext;
        _getLoad = getLoad;
        _getContext = getContext;
    }

    /// <summary>
    /// Disposes every entry created so far and clears them. Idempotent.
    /// </summary>
    public void Dispose()
    {
        this.Trace("start");

        KeyedLoaderEntry<TKey, TContext, TData>[] entries;

        lock (_locker)
        {
            if (_isDisposed)
            {
                this.Trace("skip, already disposed");
                return;
            }

            _isDisposed = true;

            this.Trace("take entries");
            entries = _entries.Values.ToArray();
            _entries.Clear();
        }

        // and drain them outside the lock: disposing an entry waits for its timers' in-flight callbacks,
        // and a callback that reaches Request would be waiting for the lock this thread holds
        foreach (var entry in entries)
        {
            this.Trace("disconnect {key} entry", entry.Key);
            entry.Dispose();
        }

        this.Trace("done");
    }

    /// <summary>
    /// Requests a load for the given key, creating and starting an entry for it if this is the first request
    /// for that key.
    /// </summary>
    /// <param name="key">The key to request a load for.</param>
    public void Request(TKey key)
    {
        this.Trace("request {key} load", key);

        KeyedLoaderEntry<TKey, TContext, TData> entry;

        lock (_locker)
        {
            // a disposed loader has already drained everything it knew about and will never drain again,
            // so an entry created now is started, bound, and left running with nothing able to stop it
            if (_isDisposed)
            {
                this.Trace("skip {key} load, already disposed", key);
                return;
            }

            if (!_entries.TryGetValue(key, out var existing))
                _entries[key] = existing = CreateLoader(key);

            entry = existing;
        }

        entry.Request();
    }

    /// <summary>
    /// Creates and starts a new entry for the given key, wiring its successful loads to update its own context
    /// and to be raised through <see cref="OnData"/>.
    /// </summary>
    /// <param name="key">The key to create an entry for.</param>
    /// <returns>The newly created, started entry.</returns>
    private KeyedLoaderEntry<TKey, TContext, TData> CreateLoader(TKey key)
    {
        this.Trace("create {key} entry", key);
        var entry = new KeyedLoaderEntry<TKey, TContext, TData>(
            key,
            _initialContext,
            _config,
            _getLoad,
            _sp.Resolve<IStatusReporter>(),
            Logger
        );
        entry.OnData += data =>
        {
            var context = _getContext(entry.Key, entry.Context, data);
            OnData(entry.Key, entry.Context, data);
            entry.UpdateContext(context);
        };

        this.Trace("start {key} loader", key);
        entry.Start();

        this.Trace("done {key} entry", key);

        return entry;
    }
}
