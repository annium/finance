using System;
using System.Threading;
using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Connectors.Connectors;
using Annium.Finance.Providers.Abstractions.Domain.Operations;
using Annium.Finance.Providers.Shared.Connectors;
using Annium.Finance.Providers.Shared.Loaders;
using Annium.Logging;
using Annium.Threading;

namespace Annium.Finance.Providers.Shared.Internal.Loaders;

internal class SnapshotLoader<T> : ISnapshotLoader<T>, ILogSubject
{
    public ILogger Logger { get; }
    public event Action<T> OnData = delegate { };
    private readonly SnapshotLoaderConfig _cfg;
    private readonly Func<CancellationToken, Task<IBaseResult<T?>>> _load;
    private readonly IStatusReporter _statusReporter;
    private readonly ISequentialTimer _timer;
    private readonly object _locker = new();
    private State _state;
    private CancellationTokenSource _cts = new();
    private int _requestCounter;

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
        }

        this.Trace("done");
    }

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

    private async ValueTask FetchSnapshotAsync()
    {
        this.Trace("start");

        // try to load snapshot - timer is not expected to be switched off at this moment
        var result = await _load(_cts.Token);

        lock (_locker)
        {
            if (_cts.IsCancellationRequested)
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
                _cts.Cancel();

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
                        this.Error(result.Message);
                }
            }
        }

        this.Trace("done");
    }

    private enum State
    {
        Inactive,
        Active,
        Disposed,
    }
}
