using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Annium.Finance.Providers.Core.Shared.Status;
using Annium.Finance.Providers.Core.Shared.TimeSync;
using Annium.Logging;
using Annium.Threading;
using NodaTime;

namespace Annium.Finance.Providers.Core.Internal.Shared.TimeSync;

/// <summary>
/// Default <see cref="IServerTimeSource"/> implementation. Refreshes server time on a timer: aggressively, at
/// <see cref="ServerTimeProviderConfig.LoadInterval"/>, until the first successful load, then at the slower
/// <see cref="ServerTimeProviderConfig.ConfirmInterval"/> to periodically confirm it is still accurate; any
/// failed refresh while confirming switches back to the load interval. Between refreshes,
/// <see cref="ServerTime"/> is extrapolated forward from the last successful value using a stopwatch.
/// </summary>
internal class ServerTimeSource : IServerTimeSource, IDisposable, ILogSubject
{
    /// <summary>Gets the logger instance.</summary>
    public ILogger Logger { get; }

    /// <summary>
    /// Gets the current server time, as Unix milliseconds, extrapolated from the last successful refresh using
    /// the elapsed time on <see cref="_watch"/>. Initialized to the local system time until the first refresh
    /// completes.
    /// </summary>
    public long ServerTime
    {
        get => field + _watch.ElapsedMilliseconds;
        private set;
    } = SystemClock.Instance.GetCurrentInstant().ToUnixTimeMilliseconds();

    /// <summary>The underlying provider used to fetch server time.</summary>
    private readonly IServerTimeProvider _provider;

    /// <summary>The timing configuration for refresh retries.</summary>
    private readonly ServerTimeProviderConfig _config;

    /// <summary>The status reporter this source's connection status is bound to.</summary>
    private readonly IStatusReporter _statusReporter;

    /// <summary>Measures elapsed time since the last successful refresh, used to extrapolate <see cref="ServerTime"/>.</summary>
    private readonly Stopwatch _watch = new();

    /// <summary>The timer that drives repeated refresh attempts.</summary>
    private readonly ISequentialTimer _timer;

    /// <summary>Cancels any in-flight refresh when this source is disposed.</summary>
    private readonly CancellationTokenSource _cts = new();

    /// <summary>Whether the timer is currently refreshing to obtain the first successful load, or confirming an already-loaded value.</summary>
    private Mode _mode = Mode.Load;

    /// <summary>
    /// Initializes a new instance of the <see cref="ServerTimeSource"/> class, binds its connection status to
    /// <paramref name="statusReporter"/>, and starts refreshing immediately.
    /// </summary>
    /// <param name="provider">The underlying provider used to fetch server time.</param>
    /// <param name="config">The timing configuration for refresh retries.</param>
    /// <param name="statusReporter">The status reporter to bind this source's connection status to.</param>
    /// <param name="logger">The logger instance.</param>
    public ServerTimeSource(
        IServerTimeProvider provider,
        ServerTimeProviderConfig config,
        IStatusReporter statusReporter,
        ILogger logger
    )
    {
        Logger = logger;
        _provider = provider;
        _config = config;
        _statusReporter = statusReporter;

        _statusReporter.Bind(this);
        _statusReporter.Connecting();

        _watch.Start();
        _timer = Timers.Async(RefreshAsync, 0, _config.LoadInterval, logger);
    }

    /// <summary>
    /// Stops the refresh timer, cancels any in-flight refresh, and reports a disconnected status.
    /// </summary>
    public void Dispose()
    {
        this.Trace("start");

        _watch.Stop();
        _timer.Dispose();
        _cts.Cancel();
        _cts.Dispose();

        _statusReporter.Disconnected();

        // and stop counting: a disposed component is gone, not disconnected. Left registered, it sits
        // in the monitor as a disconnected target beside the live ones, and the connector can never
        // report itself connected again for as long as it lives
        _statusReporter.Unbind();

        this.Trace("done");
    }

    /// <summary>
    /// Performs a single refresh. On success, updates <see cref="ServerTime"/>, restarts the extrapolation
    /// stopwatch, reports a connected status, and switches the timer to the confirm interval if it was still
    /// loading. On failure, reports a connecting status and, if a confirm attempt just failed, switches the
    /// timer back to the load interval.
    /// </summary>
    /// <returns>A task that completes once the refresh and its result have been handled.</returns>
    private async ValueTask RefreshAsync()
    {
        this.Trace("start");

        this.Trace("load server time");
        var result = await _provider.LoadAsync(_cts.Token);

        if (!result.IsSuccess)
        {
            this.Trace("server time load failed ({result})", result);

            _statusReporter.Connecting();

            if (_mode is Mode.Confirm)
            {
                this.Trace("switch to load mode");
                _mode = Mode.Load;
                _timer.Change(_config.LoadInterval, _config.LoadInterval);
            }

            this.Trace("end");

            return;
        }

        this.Trace("update server time and restart watch");
        ServerTime = result.Data;
        _watch.Restart();

        _statusReporter.Connected();

        if (_mode is Mode.Load)
        {
            this.Trace("switch to confirm mode");
            _mode = Mode.Confirm;
            _timer.Change(_config.ConfirmInterval, _config.ConfirmInterval);
        }

        this.Trace("end");
    }

    /// <summary>The refresh modes a <see cref="ServerTimeSource"/> can be in.</summary>
    private enum Mode
    {
        /// <summary>Refreshing at <see cref="ServerTimeProviderConfig.LoadInterval"/>, before the first successful load.</summary>
        Load,

        /// <summary>Refreshing at <see cref="ServerTimeProviderConfig.ConfirmInterval"/>, to confirm an already-loaded value.</summary>
        Confirm,
    }
}
