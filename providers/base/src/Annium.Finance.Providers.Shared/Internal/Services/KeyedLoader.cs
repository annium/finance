using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Annium.Core.DependencyInjection;
using Annium.Finance.Providers.Abstractions.Domain.Operations;
using Annium.Finance.Providers.Shared.Connectors;
using Annium.Finance.Providers.Shared.Services;
using Annium.Logging;

namespace Annium.Finance.Providers.Shared.Internal.Services;

internal sealed class KeyedLoader<TKey, TContext, TData> : IKeyedLoader<TKey, TContext, TData>, ILogSubject
    where TKey : notnull
{
    public ILogger Logger { get; }
    public event Action<TKey, TData> OnData = delegate { };
    private readonly IServiceProvider _sp;
    private readonly Func<TKey, TContext, CancellationToken, Task<IBaseResult<TData>>> _getLoad;
    private readonly Func<TKey, TContext, TData, TContext> _getContext;
    private readonly int _intervalPeriod;
    private readonly int _debouncePeriod;
    private readonly ConcurrentDictionary<TKey, KeyedLoaderEntry<TKey, TContext, TData>> _entries = new();
    private readonly TContext _initialContext;
    private readonly SnapshotLoaderConfig _loaderConfig;
    private State _state;

    public KeyedLoader(
        IServiceProvider sp,
        TContext initialContext,
        SnapshotLoaderConfig loaderConfig,
        Func<TKey, TContext, CancellationToken, Task<IBaseResult<TData>>> getLoad,
        Func<TKey, TContext, TData, TContext> getContext,
        int intervalPeriod,
        int debouncePeriod,
        ILogger logger
    )
    {
        Logger = logger;
        _sp = sp;
        _initialContext = initialContext;
        _loaderConfig = loaderConfig;
        _getLoad = getLoad;
        _getContext = getContext;
        _intervalPeriod = intervalPeriod;
        _debouncePeriod = debouncePeriod;
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
            entry.Value.Loader.Start();
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

    public void RequestUpdate(TKey key)
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
            _loaderConfig,
            _getLoad,
            _sp.Resolve<IStatusReporter>(),
            Logger,
            _intervalPeriod,
            _debouncePeriod
        );
        var loader = entry.Loader;
        loader.OnData += data =>
        {
            var context = _getContext(entry.Key, entry.Context, data);
            entry.UpdateContext(context);
            OnData(entry.Key, data);
        };

        this.Trace("start {key} loader", key);
        loader.Start();

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
