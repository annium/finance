using System;
using System.Threading;
using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Connectors.Shared;
using Annium.Finance.Providers.Abstractions.Domain.Shared.Operations;
using Annium.Finance.Providers.Core.Shared.Loaders;
using Annium.Finance.Providers.Core.Shared.Status;
using Annium.Logging;
using Annium.Threading;

namespace Annium.Finance.Providers.Core.Internal.Shared.Loaders;

/// <summary>
/// Default <see cref="ISnapshotLoader{T}"/> implementation. Fetches on a timer starting at
/// <see cref="SnapshotLoaderConfig.FastInterval"/>; once <see cref="SnapshotLoaderConfig.FastRequestsLimit"/>
/// consecutive fetches have been attempted without success, it switches to
/// <see cref="SnapshotLoaderConfig.SlowInterval"/> and keeps retrying at that slower pace until a fetch
/// succeeds. A successful fetch reports <see cref="ConnectorStatus.Connected"/>, raises <see cref="OnData"/>
/// once, and stops the timer; <see cref="Stop"/> cancels any in-flight fetch and discards its result.
/// </summary>
/// <typeparam name="T">The type of data loaded.</typeparam>
internal class SnapshotLoader<T> : ISnapshotLoader<T>, ILogSubject
{
    /// <summary>Gets the logger instance.</summary>
    public ILogger Logger { get; }

    /// <summary>Raised with the loaded data every time a fetch succeeds.</summary>
    public event Action<T> OnData = delegate { };

    /// <summary>The timing configuration for fetch retries.</summary>
    private readonly SnapshotLoaderConfig _cfg;

    /// <summary>The delegate that performs a single fetch.</summary>
    private readonly Func<CancellationToken, Task<IBaseResult<T?>>> _load;

    /// <summary>The status reporter this loader's connection status is bound to.</summary>
    private readonly IStatusReporter _statusReporter;

    /// <summary>The timer that drives repeated fetch attempts.</summary>
    private readonly ISequentialTimer _timer;

    /// <summary>Synchronizes access to the loader's mutable state across the timer callback and public methods.</summary>
    private readonly Lock _locker = new();

    /// <summary>The loader's current lifecycle state.</summary>
    private State _state;

    /// <summary>Cancels the fetch(es) belonging to the current <see cref="Start"/>/<see cref="Stop"/> cycle.</summary>
    private CancellationTokenSource _cts = new();

    /// <summary>Counts fetch attempts made since the last <see cref="Start"/>, used to decide when to switch to the slow interval.</summary>
    private int _requestCounter;

    /// <summary>
    /// Initializes a new instance of the <see cref="SnapshotLoader{T}"/> class and binds its connection status
    /// to <paramref name="statusReporter"/>.
    /// </summary>
    /// <param name="cfg">The timing configuration for fetch retries.</param>
    /// <param name="load">The delegate that performs a single fetch.</param>
    /// <param name="statusReporter">The status reporter to bind this loader's connection status to.</param>
    /// <param name="initialStatus">The initial connection status to report before the first fetch completes.</param>
    /// <param name="logger">The logger instance.</param>
    public SnapshotLoader(
        SnapshotLoaderConfig cfg,
        Func<CancellationToken, Task<IBaseResult<T?>>> load,
        IStatusReporter statusReporter,
        ConnectorStatus initialStatus,
        ILogger logger
    )
    {
        Logger = logger;
        _cfg = cfg;
        _load = load;
        _statusReporter = statusReporter;
        _statusReporter.Bind(this, initialStatus);
        _timer = Timers.Async(FetchSnapshotAsync, Timeout.Infinite, Timeout.Infinite, logger);
    }

    /// <summary>
    /// Cancels any in-flight fetch, stops the timer, and reports a disconnected status. Idempotent.
    /// </summary>
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

            this.Trace("cancel cts");
            _cts.Cancel();

            this.Trace("dispose timer");
            _timer.Dispose();

            this.Trace("signal disconnected state");
            _statusReporter.Disconnected();

            // and stop counting: a disposed component is gone, not disconnected. Left registered, it sits
            // in the monitor as a disconnected target beside the live ones, and the connector can never
            // report itself connected again for as long as it lives
            _statusReporter.Unbind();
        }

        this.Trace("done");
    }

    /// <summary>
    /// Starts fetching immediately, then on the fast interval, until a fetch succeeds or the loader is stopped.
    /// Has no effect unless the loader is currently inactive.
    /// </summary>
    /// <param name="reportStatus">Whether to report a connecting status while fetching.</param>
    public void Start(bool reportStatus)
    {
        this.Trace("start");

        lock (_locker)
        {
            if (_state is not State.Inactive)
            {
                this.Trace("can't start from {state} state", _state);
                return;
            }

            _state = State.Active;
            _cts = new();
            _requestCounter = 0;

            if (reportStatus)
            {
                this.Trace("signal connecting state");
                _statusReporter.Connecting();
            }

            _timer.Change(0, _cfg.FastInterval);
        }

        this.Trace("done");
    }

    /// <summary>
    /// Cancels any in-flight fetch, whose result is then discarded, and stops the timer. Has no effect unless
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

            if (_cts.IsCancellationRequested)
            {
                this.Trace("already stopped");
                return;
            }

            this.Trace("change state to {state}", State.Inactive);
            _state = State.Inactive;

            this.Trace("cancel cts");
            _cts.Cancel();

            this.Trace("signal connecting state");
            _statusReporter.Connecting();

            this.Trace("stop timer");
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        this.Trace("done");
    }

    /// <summary>
    /// Performs a single fetch and, on success, raises <see cref="OnData"/> and stops the timer; on failure,
    /// switches to the slow interval once <see cref="SnapshotLoaderConfig.FastRequestsLimit"/> has been reached
    /// and logs the failure message, if any.
    /// </summary>
    /// <returns>A task that completes once the fetch and its result have been handled.</returns>
    private async ValueTask FetchSnapshotAsync()
    {
        this.Trace("start");

        // the source this fetch is issued under, held for the whole call: Start replaces the field with a
        // fresh one, so a fetch left over from a stopped cycle would otherwise ask the *new* cycle's source
        // whether it was cancelled, be told no, and deliver its stale answer as if it were the new one's
        var cts = _cts;

        // try to load snapshot - timer is not expected to be switched off at this moment
        var result = await _load(cts.Token);

        lock (_locker)
        {
            if (cts.IsCancellationRequested)
            {
                this.Trace("already canceled");
                return;
            }

            // increment request counter if response is being processed
            _requestCounter++;

            if (result.IsSuccess)
            {
                this.Trace("change state to {state}", State.Inactive);
                _state = State.Inactive;

                this.Trace("cancel cts");
#pragma warning disable VSTHRD103
                cts.Cancel();
#pragma warning restore VSTHRD103

                this.Trace("stop timer");
                _timer.Change(Timeout.Infinite, Timeout.Infinite);

                this.Trace("send data");
                OnData(result.Data);

                // signal connected state always
                // this won't trigger invalid connector state, but will correctly handle case,
                // when snapshot load fails without socket disconnect
                this.Trace("signal connected state");
                _statusReporter.Connected();
            }
            else
            {
                if (_requestCounter >= _cfg.FastRequestsLimit)
                {
                    this.Trace("switch to long-delayed snapshot requests");
                    _timer.Change(_cfg.SlowInterval, _cfg.SlowInterval);
                }

                this.Trace("signal connecting state");
                _statusReporter.Connecting();

                // !aborted -> failed
                if (!result.IsAborted)
                {
                    this.Trace("write error");
                    if (!result.Message.IsNullOrWhiteSpace())
                        this.Error<string>("snapshot load failed: {message}", result.Message);
                }
            }
        }

        this.Trace("done");
    }

    /// <summary>The lifecycle states an <see cref="SnapshotLoader{T}"/> can be in.</summary>
    private enum State
    {
        /// <summary>The loader is not fetching, either before the first <see cref="Start"/> or after <see cref="Stop"/>.</summary>
        Inactive,

        /// <summary>The loader is fetching, on either the fast or the slow interval.</summary>
        Active,

        /// <summary>The loader has been disposed and can no longer be started.</summary>
        Disposed,
    }
}
