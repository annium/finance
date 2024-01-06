using System;
using System.Threading;
using System.Threading.Tasks;
using Annium.Finance.Providers.Shared.Loaders;
using Annium.Logging;
using Annium.Threading;

namespace Annium.Finance.Providers.Shared.Internal.Loaders;

internal class CompositeLoader<T> : ICompositeLoader<T>, ILogSubject
{
    public ILogger Logger { get; }
    public event Action<T> OnData = delegate { };
    private readonly ISnapshotLoader<T> _loader;
    private readonly IAsyncTimer? _intervalTimer;
    private readonly int _intervalPeriod;
    private readonly IDebounceTimer? _debounceTimer;
    private readonly int _debouncePeriod;
    private readonly object _locker = new();
    private State _state;

    public CompositeLoader(ISnapshotLoader<T> loader, int intervalPeriod, int debouncePeriod, ILogger logger)
    {
        Logger = logger;
        _intervalPeriod = intervalPeriod;
        _debouncePeriod = debouncePeriod;

        _loader = loader;
        _loader.OnData += HandleData;

        if (intervalPeriod != Timeout.Infinite)
        {
            this.Trace("create interval timer with period {0}", intervalPeriod);
            _intervalTimer = Timers.Async(InitIntervalLoad, Timeout.Infinite, Timeout.Infinite, logger);
        }
        else
        {
            this.Trace("no interval timer created");
        }

        if (debouncePeriod != Timeout.Infinite)
        {
            this.Trace("create debounce timer with period {0}", debouncePeriod);
            _debounceTimer = Timers.Debounce(InitDebounceLoad, Timeout.Infinite, logger);
        }
        else
        {
            this.Trace("no debounce timer created");
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

            if (_intervalTimer is not null)
            {
                this.Trace("dispose interval timer");
                _intervalTimer.Dispose();
            }

            if (_debounceTimer is not null)
            {
                this.Trace("dispose debounce timer");
                _debounceTimer.Dispose();
            }
        }

        this.Trace("done");
    }

    public void Start()
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
            _loader.Start(reportStatus: true);

            if (_intervalTimer is not null)
            {
                this.Trace("start interval timer");
                _intervalTimer.Change(_intervalPeriod, _intervalPeriod);
            }

            if (_debounceTimer is not null)
            {
                this.Trace("start debounce timer");
                _debounceTimer.Change(_debouncePeriod);
            }
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

            if (_intervalTimer is not null)
            {
                this.Trace("stop interval timer");
                _intervalTimer.Change(Timeout.Infinite, Timeout.Infinite);
            }

            if (_debounceTimer is not null)
            {
                this.Trace("stop debounce timer");
                _debounceTimer.Change(Timeout.Infinite);
            }
        }

        this.Trace("done");
    }

    public void Request()
    {
        this.Trace("start");

        lock (_locker)
        {
            if (_state is not State.Active)
            {
                this.Trace("can't request from {state} state", _state);
                return;
            }

            if (_debounceTimer is null)
            {
                throw new InvalidOperationException("Debounce timer was not created (infinite period specified)");
            }

            this.Trace("request update on debounce timer");
            _debounceTimer.Request();
        }

        this.Trace("done");
    }

    private ValueTask InitIntervalLoad()
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

    private ValueTask InitDebounceLoad()
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
        Disposed
    }
}
