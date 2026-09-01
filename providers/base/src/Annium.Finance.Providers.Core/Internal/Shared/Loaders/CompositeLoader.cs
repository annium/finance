using System;
using System.Threading;
using System.Threading.Tasks;
using Annium.Finance.Providers.Core.Shared.Loaders;
using Annium.Logging;
using Annium.Threading;

namespace Annium.Finance.Providers.Core.Internal.Shared.Loaders;

/// <summary>
/// Default <see cref="ICompositeLoader{T}"/> implementation. Wraps an <see cref="ISnapshotLoader{T}"/>, keeping
/// it started for as long as the composite loader is active, and additionally triggers reloads (without
/// reporting status) on a fixed interval and on debounced <see cref="Request"/> calls. When either
/// <c>intervalPeriod</c> or <c>debouncePeriod</c> is zero or negative, the corresponding
/// trigger is disabled via a no-op timer. A failed reload is retried internally by the underlying snapshot
/// loader and never stops the composite loader; only <see cref="Stop"/> or <see cref="Dispose"/> do.
/// </summary>
/// <typeparam name="T">The type of data loaded.</typeparam>
internal class CompositeLoader<T> : ICompositeLoader<T>, ILogSubject
{
    /// <summary>Gets the logger instance.</summary>
    public ILogger Logger { get; }

    /// <summary>Raised with the loaded data every time a reload succeeds.</summary>
    public event Action<T> OnData = delegate { };

    /// <summary>The underlying snapshot loader that performs each individual load.</summary>
    private readonly ISnapshotLoader<T> _loader;

    /// <summary>The timer that triggers a reload on a fixed interval while active, or a no-op if disabled.</summary>
    private readonly ISequentialTimer _intervalTimer;

    /// <summary>The interval, in milliseconds, between interval-triggered reloads.</summary>
    private readonly int _intervalPeriod;

    /// <summary>The timer that debounces <see cref="Request"/> calls into a single reload, or a no-op if disabled.</summary>
    private readonly IDebounceTimer _debounceTimer;

    /// <summary>The debounce period, in milliseconds, applied to <see cref="Request"/> calls.</summary>
    private readonly int _debouncePeriod;

    /// <summary>Synchronizes access to the loader's mutable state across timer callbacks and public methods.</summary>
    private readonly Lock _locker = new();

    /// <summary>The loader's current lifecycle state.</summary>
    private State _state;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompositeLoader{T}"/> class, wiring it to the underlying
    /// snapshot loader's data and creating the interval and debounce timers (or their no-op equivalents).
    /// </summary>
    /// <param name="loader">The underlying snapshot loader that performs each individual load.</param>
    /// <param name="intervalPeriod">The interval, in milliseconds, between interval-triggered reloads; zero or negative disables this trigger.</param>
    /// <param name="debouncePeriod">The debounce period, in milliseconds, applied to <see cref="Request"/> calls; zero or negative disables this trigger.</param>
    /// <param name="logger">The logger instance.</param>
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

    /// <summary>
    /// Disposes the underlying snapshot loader and both timers. Idempotent.
    /// </summary>
    public void Dispose()
    {
        this.Trace("start");

        // flag and unhook under the lock, drain outside it. Disposing a timer waits for an in-flight
        // callback to finish, and both callbacks take this same lock as their first act - so draining while
        // holding it left the callback waiting on the disposing thread and the disposing thread waiting on
        // the callback, until the drain budget ran out and leaked the wait handle. Bounded, but seconds per
        // timer, and KeyedLoader disposes its entries one after another
        lock (_locker)
        {
            if (_state is State.Disposed)
            {
                this.Trace("already {state}", _state);
                return;
            }

            this.Trace("set is disposed");
            _state = State.Disposed;

            this.Trace("unhook loader data");
            _loader.OnData -= HandleData;
        }

        this.Trace("dispose loader");
        _loader.Dispose();

        this.Trace("dispose interval timer");
        _intervalTimer.Dispose();

        this.Trace("dispose debounce timer");
        _debounceTimer.Dispose();

        this.Trace("done");
    }

    /// <summary>
    /// Starts the underlying snapshot loader and arms the interval and debounce timers. Has no effect unless the
    /// loader is currently inactive or stopped.
    /// </summary>
    /// <param name="reportStatus">Whether to report a connecting status for the initial load.</param>
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

    /// <summary>
    /// Stops the underlying snapshot loader and disarms the interval and debounce timers. Has no effect unless
    /// the loader is currently active.
    /// </summary>
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

    /// <summary>
    /// Requests a reload via the debounce timer. Multiple calls within the debounce period collapse into a
    /// single reload. Has no effect unless the loader is currently active.
    /// </summary>
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

            this.Trace("request update on debounce timer");
            _debounceTimer.Request();
        }

        this.Trace("done");
    }

    /// <summary>
    /// Callback for the interval timer: starts the underlying snapshot loader without reporting status, if the
    /// composite loader is still active.
    /// </summary>
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

    /// <summary>
    /// Callback for the debounce timer: starts the underlying snapshot loader without reporting status, if the
    /// composite loader is still active.
    /// </summary>
    /// <returns>A completed task; the loader's own fetch runs independently of this callback.</returns>
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

    /// <summary>Forwards loaded data from the underlying snapshot loader through <see cref="OnData"/>.</summary>
    /// <param name="data">The loaded data.</param>
    private void HandleData(T data) => OnData(data);

    /// <summary>The lifecycle states a <see cref="CompositeLoader{T}"/> can be in.</summary>
    private enum State
    {
        /// <summary>The loader has not been started yet.</summary>
        Inactive,

        /// <summary>The loader is active: the underlying snapshot loader and both timers are running.</summary>
        Active,

        /// <summary>The loader was started and then explicitly stopped via <see cref="Stop"/>.</summary>
        Stopped,

        /// <summary>The loader has been disposed and can no longer be started.</summary>
        Disposed,
    }
}

/// <summary>
/// A no-op <see cref="ISequentialTimer"/> used by <see cref="CompositeLoader{T}"/> in place of a real interval
/// timer when interval-triggered reloads are disabled.
/// </summary>
file class NoopSequentialTimer : ISequentialTimer
{
    /// <summary>Gets the shared no-op instance.</summary>
    public static ISequentialTimer Instance { get; } = new NoopSequentialTimer();

    /// <summary>Initializes a new instance of the <see cref="NoopSequentialTimer"/> class.</summary>
    private NoopSequentialTimer() { }

    /// <summary>Does nothing.</summary>
    public void Dispose() { }

    /// <summary>Does nothing.</summary>
    /// <returns>A completed task.</returns>
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    /// <summary>Does nothing.</summary>
    /// <param name="dueTime">Ignored.</param>
    /// <param name="period">Ignored.</param>
    /// <returns><see langword="true"/>, always.</returns>
    public bool Change(int dueTime, int period)
    {
        return true;
    }

    /// <summary>Does nothing.</summary>
    /// <param name="dueTime">Ignored.</param>
    /// <param name="period">Ignored.</param>
    /// <returns><see langword="true"/>, always.</returns>
    public bool Change(TimeSpan dueTime, TimeSpan period)
    {
        return true;
    }
}

/// <summary>
/// A no-op <see cref="IDebounceTimer"/> used by <see cref="CompositeLoader{T}"/> in place of a real debounce
/// timer when debounce-triggered reloads are disabled.
/// </summary>
file class NoopDebounceTimer : IDebounceTimer
{
    /// <summary>Gets the shared no-op instance.</summary>
    public static IDebounceTimer Instance { get; } = new NoopDebounceTimer();

    /// <summary>Initializes a new instance of the <see cref="NoopDebounceTimer"/> class.</summary>
    private NoopDebounceTimer() { }

    /// <summary>Does nothing.</summary>
    public void Dispose() { }

    /// <summary>Does nothing.</summary>
    /// <returns>A completed task.</returns>
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    /// <summary>Does nothing.</summary>
    /// <param name="period">Ignored.</param>
    public void Change(int period) { }

    /// <summary>Does nothing.</summary>
    /// <param name="period">Ignored.</param>
    public void Change(TimeSpan period) { }

    /// <summary>Does nothing.</summary>
    public void Request() { }
}
