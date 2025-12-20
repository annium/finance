using System;
using System.Threading;
using System.Threading.Tasks;
using Annium.Finance.Providers.Core.Shared.Loaders;
using Annium.Logging;
using Annium.Threading;

namespace Annium.Finance.Providers.Core.Internal.Shared.Loaders;

internal class CompositeLoader<T> : ICompositeLoader<T>, ILogSubject
{
    public ILogger Logger { get; }
    public event Action<T> OnData = delegate { };
    private readonly ISnapshotLoader<T> _loader;
    private readonly ISequentialTimer _intervalTimer;
    private readonly int _intervalPeriod;
    private readonly IDebounceTimer _debounceTimer;
    private readonly int _debouncePeriod;
    private readonly Lock _locker = new();
    private State _state;

    public CompositeLoader(ISnapshotLoader<T> loader, int intervalPeriod, int debouncePeriod, ILogger logger)
    {
        Logger = logger;
        _intervalPeriod = intervalPeriod;
        _debouncePeriod = debouncePeriod;

        _loader = loader;
        _loader.OnData += HandleData;

        if (intervalPeriod > 0)
        {
            this.Trace("create interval timer with period {0}", intervalPeriod);
            _intervalTimer = Timers.Sync(InitIntervalLoad, Timeout.Infinite, Timeout.Infinite, logger);
        }
        else
        {
            this.Trace("create noop interval timer");
            _intervalTimer = NoopSequentialTimer.Instance;
        }

        if (debouncePeriod > 0)
        {
            this.Trace("create debounce timer with period {0}", debouncePeriod);
            _debounceTimer = Timers.Debounce(InitDebounceLoadAsync, Timeout.Infinite, logger);
        }
        else
        {
            this.Trace("create noop debounce timer");
            _debounceTimer = NoopDebounceTimer.Instance;
        }
    }

    public void Dispose()
    {
        this.Trace("start");

        lock (_locker)
        {
            if (_state is State.Disposed)
            {
                this.Trace("already {state}", _state);
                return;
            }

            this.Trace("set is disposed");
            _state = State.Disposed;

            this.Trace("dispose loader");
            _loader.OnData -= HandleData;
            _loader.Dispose();

            this.Trace("dispose interval timer");
            _intervalTimer.Dispose();

            this.Trace("dispose debounce timer");
            _debounceTimer.Dispose();
        }

        this.Trace("done");
    }

    public void Start(bool reportStatus)
    {
        this.Trace("start");

        lock (_locker)
        {
            if (_state is not (State.Inactive or State.Stopped))
            {
                this.Trace("can't start from {state} state", _state);
                return;
            }

            _state = State.Active;

            this.Trace("start loader");
            _loader.Start(reportStatus);

            this.Trace("start interval timer");
            _intervalTimer.Change(_intervalPeriod, _intervalPeriod);

            this.Trace("start debounce timer");
            _debounceTimer.Change(_debouncePeriod);
        }

        this.Trace("done");
    }

    public void Stop()
    {
        this.Trace("start");

        lock (_locker)
        {
            if (_state is not State.Active)
            {
                this.Trace("can't stop from {state} state", _state);
                return;
            }

            _state = State.Stopped;

            this.Trace("stop loader");
            _loader.Stop();

            this.Trace("stop interval timer");
            _intervalTimer.Change(Timeout.Infinite, Timeout.Infinite);

            this.Trace("stop debounce timer");
            _debounceTimer.Change(Timeout.Infinite);
        }

        this.Trace("done");
    }

    public void Request()
    {
        if (_debounceTimer is null)
            throw new InvalidOperationException("Debounce timer was not created (infinite period specified)");

        this.Trace("start");

        lock (_locker)
        {
            if (_state is not State.Active)
            {
                this.Trace("can't request from {state} state", _state);
                return;
            }

            this.Trace("request update on debounce timer");
            _debounceTimer.Request();
        }

        this.Trace("done");
    }

    private void InitIntervalLoad()
    {
        this.Trace("start");

        lock (_locker)
        {
            if (_state is not State.Active)
            {
                this.Trace("can't request from {state} state", _state);
                return;
            }

            this.Trace("start loader");
            _loader.Start(reportStatus: false);
        }

        this.Trace("done");
    }

    private ValueTask InitDebounceLoadAsync()
    {
        this.Trace("start");

        lock (_locker)
        {
            if (_state is not State.Active)
            {
                this.Trace("can't request from {state} state", _state);
                return ValueTask.CompletedTask;
            }

            this.Trace("start loader");
            _loader.Start(reportStatus: false);
        }

        this.Trace("done");

        return ValueTask.CompletedTask;
    }

    private void HandleData(T data) => OnData(data);

    private enum State
    {
        Inactive,
        Active,
        Stopped,
        Disposed,
    }
}

file class NoopSequentialTimer : ISequentialTimer
{
    public static ISequentialTimer Instance { get; } = new NoopSequentialTimer();

    private NoopSequentialTimer() { }

    public void Dispose() { }

    public bool Change(int dueTime, int period)
    {
        return true;
    }

    public bool Change(TimeSpan dueTime, TimeSpan period)
    {
        return true;
    }
}

file class NoopDebounceTimer : IDebounceTimer
{
    public static IDebounceTimer Instance { get; } = new NoopDebounceTimer();

    private NoopDebounceTimer() { }

    public void Dispose() { }

    public ValueTask DisposeAsync()
    {
        // TODO: remove temp
        return ValueTask.CompletedTask;
    }

    public void Change(int period) { }

    public void Request() { }
}
