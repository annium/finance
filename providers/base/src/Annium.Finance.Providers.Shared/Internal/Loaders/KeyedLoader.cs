using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Annium.Core.DependencyInjection;
using Annium.Finance.Providers.Abstractions.Domain.Shared;
using Annium.Finance.Providers.Abstractions.Domain.Shared.Operations;
using Annium.Finance.Providers.Shared.Connectors;
using Annium.Finance.Providers.Shared.Loaders;
using Annium.Logging;

namespace Annium.Finance.Providers.Shared.Internal.Loaders;

internal sealed class KeyedLoader<TKey, TContext, TData> : IKeyedLoader<TKey, TContext, TData>, ILogSubject
    where TKey : notnull
{
    public ILogger Logger { get; }
    public event Action<TKey, TContext, TData> OnData = delegate { };
    private readonly IServiceProvider _sp;
    private readonly CompositeLoaderConfig _config;
    private readonly TContext _initialContext;
    private readonly Func<TKey, TContext, CancellationToken, Task<IBaseResult<TData?>>> _getLoad;
    private readonly Func<TKey, TContext, TData, TContext> _getContext;
    private readonly ConcurrentDictionary<TKey, KeyedLoaderEntry<TKey, TContext, TData>> _entries = new();
    private State _state;

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

    public void Dispose()
    {
        this.Trace("start");

        if (_state == State.Disposed)
        {
            this.Trace("skip, already disposed");
            return;
        }

        _state = State.Disposed;

        foreach (var entry in _entries)
        {
            this.Trace("disconnect {key} entry", entry.Key);
            entry.Value.Loader.Dispose();
        }

        this.Trace("clear entries");
        _entries.Clear();

        this.Trace("done");
    }

    public void Start()
    {
        this.Trace("start");

        if (_state != State.Inactive && _state != State.Stopped)
        {
            this.Trace("can't start from state {key}", _state);
            return;
        }

        _state = State.Active;

        foreach (var entry in _entries)
        {
            this.Trace("start {key} entry", entry.Key);
            entry.Value.Loader.Start(false);
        }

        this.Trace("done");
    }

    public void Stop()
    {
        this.Trace("start");

        if (_state != State.Active)
        {
            this.Trace("can't stop from state {key}", _state);
            return;
        }

        _state = State.Stopped;

        foreach (var entry in _entries)
        {
            this.Trace("stop {key} entry", entry.Key);
            entry.Value.Loader.Stop();
        }

        this.Trace("done");
    }

    public void Request(TKey key)
    {
        this.Trace("request {key} load", key);
        _entries.GetOrAdd(key, CreateLoader).Loader.Request();
    }

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
        var loader = entry.Loader;
        loader.OnData += data =>
        {
            var context = _getContext(entry.Key, entry.Context, data);
            OnData(entry.Key, entry.Context, data);
            entry.UpdateContext(context);
        };

        this.Trace("start {key} loader", key);
        loader.Start(false);

        this.Trace("done {key} entry", key);

        return entry;
    }

    private enum State
    {
        Inactive,
        Active,
        Stopped,
        Disposed,
    }
}
